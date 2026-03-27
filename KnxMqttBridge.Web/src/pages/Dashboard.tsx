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
        <div className="relative flex bg-slate-900 border-b border-slate-700">
          {canScrollLeft && (
            <button
              onClick={() => scrollTabs('left')}
              className="shrink-0 w-8 text-lg font-bold text-slate-300 hover:text-white
                         bg-gradient-to-r from-slate-900 via-slate-900 to-transparent z-10"
              aria-label="Nach links scrollen"
            >
              ‹
            </button>
          )}
          <div
            ref={tabBarRef}
            className="flex overflow-x-auto"
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
                onClick={(e) => handleTabClick(group, e.currentTarget)}
                className={`px-4 py-3 text-sm font-medium whitespace-nowrap shrink-0 border-b-2 transition-colors ${
                  group === activeGroup
                    ? 'border-brand-500 text-white'
                    : 'border-transparent text-slate-400 hover:text-slate-200'
                }`}
              >
                {group}
              </button>
            ))}
          </div>
          {canScrollRight && (
            <button
              onClick={() => scrollTabs('right')}
              className="shrink-0 w-8 text-lg font-bold text-slate-300 hover:text-white
                         bg-gradient-to-l from-slate-900 via-slate-900 to-transparent z-10"
              aria-label="Nach rechts scrollen"
            >
              ›
            </button>
          )}
        </div>
      )}

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-4">
        {activeItems.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full gap-4 text-center">
            <div className="text-5xl">📋</div>
            <p className="text-slate-300 text-lg font-medium">
              {orderedItems.length === 0 ? 'Keine Elemente konfiguriert' : 'Keine Elemente in dieser Gruppe'}
            </p>
            <p className="text-slate-500 text-sm">
              Gehe zu{' '}
              <Link to="/configure" className="text-brand-500 underline">
                Konfigurieren
              </Link>{' '}
              um KNX-Adressen dieser Gruppe zuzuweisen.
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
