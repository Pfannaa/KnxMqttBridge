import {
  DndContext,
  closestCenter,
  PointerSensor,
  TouchSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  rectSortingStrategy,
  useSortable,
  arrayMove,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useState, useMemo, useEffect, useRef, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { ClipboardList } from 'lucide-react';
import { AddressCard } from '../components/AddressCard';
import type { DashboardItem, UiConfig } from '../types';

interface SortableCardProps {
  item: DashboardItem;
  value: unknown;
  onPublish: (address: string, value: unknown) => void;
}

function SortableCard({ item, value, onPublish }: SortableCardProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: item.address });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    zIndex: isDragging ? 50 : undefined,
  };

  return (
    <div ref={setNodeRef} style={style}>
      <AddressCard
        item={item}
        value={value}
        onPublish={onPublish}
        dragHandleProps={{ ...attributes, ...listeners }}
      />
    </div>
  );
}

interface Props {
  items: DashboardItem[];
  values: Record<string, unknown>;
  onPublish: (address: string, value: unknown) => void;
  onReorder: (config: UiConfig) => void;
  uiConfig: UiConfig;
}

export function Dashboard({ items, values, onPublish, onReorder, uiConfig }: Props) {
  const [orderedItems, setOrderedItems] = useState(items);
  const [activeGroup, setActiveGroup] = useState<string | null>(null);
  const tabBarRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const updateScrollButtons = useCallback(() => {
    const el = tabBarRef.current;
    if (!el) return;
    setCanScrollLeft(el.scrollLeft > 0);
    setCanScrollRight(el.scrollLeft + el.clientWidth < el.scrollWidth - 1);
  }, []);

  const swipeStart = useRef<{ x: number; y: number } | null>(null);

  const scrollTabs = (dir: 'left' | 'right') => {
    tabBarRef.current?.scrollBy({ left: dir === 'left' ? -200 : 200, behavior: 'smooth' });
  };

  // Sync when items change from outside (e.g. after configure)
  useEffect(() => setOrderedItems(items), [items]);

  const grouped = useMemo(() => {
    const map = new Map<string, DashboardItem[]>();
    for (const item of orderedItems) {
      if (!map.has(item.group)) map.set(item.group, []);
      map.get(item.group)!.push(item);
    }
    return map;
  }, [orderedItems]);

  const groups = useMemo(() => {
    // Follow uiConfig.groups order; append any orphaned groups at the end
    const ordered = [...uiConfig.groups];
    for (const g of grouped.keys()) {
      if (!ordered.includes(g)) ordered.push(g);
    }
    return ordered;
  }, [grouped, uiConfig.groups]);

  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    const t = e.touches[0];
    swipeStart.current = { x: t.clientX, y: t.clientY };
  }, []);

  const handleTouchEnd = useCallback((e: React.TouchEvent) => {
    if (!swipeStart.current) return;
    const t = e.changedTouches[0];
    const dx = t.clientX - swipeStart.current.x;
    const dy = t.clientY - swipeStart.current.y;
    swipeStart.current = null;

    if (Math.abs(dx) < 50 || Math.abs(dx) < Math.abs(dy) * 1.5) return;

    setActiveGroup((current) => {
      const idx = groups.indexOf(current ?? '');
      if (dx < 0) return groups[Math.min(idx + 1, groups.length - 1)];
      return groups[Math.max(idx - 1, 0)];
    });
  }, [groups]);

  useEffect(() => {
    const el = tabBarRef.current;
    if (!el) return;
    updateScrollButtons();
    el.addEventListener('scroll', updateScrollButtons);
    const ro = new ResizeObserver(updateScrollButtons);
    ro.observe(el);
    return () => {
      el.removeEventListener('scroll', updateScrollButtons);
      ro.disconnect();
    };
  }, [groups, updateScrollButtons]);

  // Scroll active tab into view whenever it changes (e.g. via swipe)
  useEffect(() => {
    if (!activeGroup || !tabBarRef.current) return;
    const btn = tabBarRef.current.querySelector<HTMLElement>(`[data-group="${activeGroup}"]`);
    btn?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
  }, [activeGroup]);

  // Keep activeGroup valid when groups change
  useEffect(() => {
    if (groups.length === 0) {
      setActiveGroup(null);
    } else if (!activeGroup || !groups.includes(activeGroup)) {
      setActiveGroup(groups[0]);
    }
  }, [groups, activeGroup]);

  const activeItems = useMemo(
    () => (activeGroup ? (grouped.get(activeGroup) ?? []) : []),
    [grouped, activeGroup],
  );

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 200, tolerance: 8 } }),
  );

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    setOrderedItems((prev) => {
      const oldIndex = prev.findIndex((i) => i.address === active.id);
      const newIndex = prev.findIndex((i) => i.address === over.id);
      const next = arrayMove(prev, oldIndex, newIndex);

      // Persist new order
      const updatedItems = uiConfig.items
        .map((cfg) => ({ ...cfg, order: next.findIndex((i) => i.address === cfg.address) }))
        .sort((a, b) => a.order - b.order);
      onReorder({ ...uiConfig, items: updatedItems });

      return next;
    });
  };

  const handleTabClick = (group: string, btn: HTMLButtonElement) => {
    setActiveGroup(group);
    // Scroll the clicked tab into view within the tab bar
    btn.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
  };

  return (
    <div className="flex flex-col h-full">
      {/* Tab bar */}
      {groups.length > 0 && (
        <div className="relative flex items-center bg-zinc-900 border-b border-zinc-800 px-2">
          {canScrollLeft && (
            <button
              onClick={() => scrollTabs('left')}
              className="shrink-0 w-7 text-base font-bold text-zinc-500 hover:text-zinc-200
                         bg-gradient-to-r from-zinc-900 via-zinc-900 to-transparent z-10 self-stretch flex items-center justify-center"
              aria-label="Scroll left"
            >
              ‹
            </button>
          )}
          <div
            ref={tabBarRef}
            className="flex overflow-x-auto gap-1 py-2"
            style={{ scrollbarWidth: 'none' }}
            onWheel={(e) => {
              if (e.deltaY !== 0) {
                e.preventDefault();
                tabBarRef.current?.scrollBy({ left: e.deltaY, behavior: 'auto' });
              }
            }}
          >
            {groups.map((group) => (
              <button
                key={group}
                data-group={group}
                onClick={(e) => handleTabClick(group, e.currentTarget)}
                className={`px-3 h-9 text-sm font-medium whitespace-nowrap shrink-0 rounded-md transition-colors ${
                  group === activeGroup
                    ? 'bg-zinc-800 text-white'
                    : 'text-zinc-400 hover:bg-zinc-800/60 hover:text-zinc-100'
                }`}
              >
                {group}
              </button>
            ))}
          </div>
          {canScrollRight && (
            <button
              onClick={() => scrollTabs('right')}
              className="shrink-0 w-7 text-base font-bold text-zinc-500 hover:text-zinc-200
                         bg-gradient-to-l from-zinc-900 via-zinc-900 to-transparent z-10 self-stretch flex items-center justify-center"
              aria-label="Scroll right"
            >
              ›
            </button>
          )}
        </div>
      )}

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-4" onTouchStart={handleTouchStart} onTouchEnd={handleTouchEnd}>
        {activeGroup && activeItems.length > 0 && (
          <h1 className="text-2xl font-bold text-white mb-5">{activeGroup}</h1>
        )}
        {activeItems.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full gap-3 text-center">
            <ClipboardList className="w-10 h-10 text-zinc-600" strokeWidth={1.25} />
            <p className="text-zinc-300 font-medium">
              {orderedItems.length === 0 ? 'No items configured' : 'No items in this group'}
            </p>
            <p className="text-zinc-500 text-sm">
              Go to{' '}
              <Link to="/configure" className="text-brand-500 underline underline-offset-2">
                Configure
              </Link>{' '}
              to assign KNX addresses to this group.
            </p>
          </div>
        ) : (
          <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
            <SortableContext
              items={activeItems.map((i) => i.address)}
              strategy={rectSortingStrategy}
            >
              <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
                {activeItems.map((item) => (
                  <SortableCard
                    key={item.address}
                    item={item}
                    value={values[item.address]}
                    onPublish={onPublish}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        )}
      </div>
    </div>
  );
}
