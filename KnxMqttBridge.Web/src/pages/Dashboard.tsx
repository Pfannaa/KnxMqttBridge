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
import { useState, useMemo, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ConnectionStatus } from '../components/ConnectionStatus';
import { AddressCard } from '../components/AddressCard';
import type { DashboardItem, FrontendSettings, UiConfig } from '../types';

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
  connected: boolean;
  mqttError: string | null;
  settings: FrontendSettings;
  onPublish: (address: string, value: unknown) => void;
  onReorder: (config: UiConfig) => void;
  uiConfig: UiConfig;
}

export function Dashboard({ items, values, connected, mqttError, settings, onPublish, onReorder, uiConfig }: Props) {
  const [orderedItems, setOrderedItems] = useState(items);

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

  return (
    <div className="flex flex-col h-full">
      {/* Status bar */}
      <div className="flex items-center justify-between px-4 py-2 bg-slate-900 border-b border-slate-700">
        <ConnectionStatus
          connected={connected}
          error={mqttError}
          host={settings.mqttBrokerHost}
          port={settings.mqttWebSocketPort}
        />
        <span className="text-slate-500 text-sm">{orderedItems.length} Elemente</span>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-4">
        {orderedItems.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full gap-4 text-center">
            <div className="text-5xl">📋</div>
            <p className="text-slate-300 text-lg font-medium">Keine Elemente konfiguriert</p>
            <p className="text-slate-500 text-sm">
              Gehe zu{' '}
              <Link to="/configure" className="text-brand-500 underline">
                Konfigurieren
              </Link>{' '}
              um KNX-Adressen auszuwählen.
            </p>
          </div>
        ) : (
          <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
            <SortableContext
              items={orderedItems.map((i) => i.address)}
              strategy={rectSortingStrategy}
            >
              <div className="space-y-6">
                {Array.from(grouped.entries()).map(([group, groupItems]) => (
                  <section key={group}>
                    <h2 className="text-brand-500 font-bold text-sm uppercase tracking-wider mb-3">
                      {group}
                    </h2>
                    <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
                      {groupItems.map((item) => (
                        <SortableCard
                          key={item.address}
                          item={item}
                          value={values[item.address]}
                          onPublish={onPublish}
                        />
                      ))}
                    </div>
                  </section>
                ))}
              </div>
            </SortableContext>
          </DndContext>
        )}
      </div>
    </div>
  );
}
