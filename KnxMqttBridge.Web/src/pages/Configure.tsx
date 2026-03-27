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
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useState, useMemo } from 'react';
import type { KnxAddress, UiConfig, UiConfigItem } from '../types';

function SortableGroupRow({ name, onRemove }: { name: string; onRemove: () => void }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: name });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="flex items-center gap-2 bg-slate-800 border border-slate-600 rounded-lg px-3 py-2"
    >
      <button
        {...attributes}
        {...listeners}
        className="text-slate-500 hover:text-slate-300 cursor-grab active:cursor-grabbing touch-none"
        aria-label="Ziehen zum Sortieren"
      >
        ⠿
      </button>
      <span className="flex-1 text-white text-sm">{name}</span>
      <button
        onClick={onRemove}
        className="text-slate-500 hover:text-red-400 text-lg leading-none"
        aria-label={`${name} entfernen`}
      >
        ×
      </button>
    </div>
  );
}

interface Props {
  addresses: KnxAddress[];
  uiConfig: UiConfig;
  onSave: (config: UiConfig) => void;
}

export function Configure({ addresses, uiConfig, onSave }: Props) {
  const [groups, setGroups] = useState<string[]>(uiConfig.groups ?? []);
  const [newGroup, setNewGroup] = useState('');
  const [selected, setSelected] = useState<Map<string, UiConfigItem>>(() => {
    const map = new Map<string, UiConfigItem>();
    uiConfig.items.forEach((item) => map.set(item.address, item));
    return map;
  });
  const [saved, setSaved] = useState(false);
  const [search, setSearch] = useState('');

  const xmlGrouped = useMemo(() => {
    const filtered = addresses.filter(
      (a) =>
        search === '' ||
        a.name.toLowerCase().includes(search.toLowerCase()) ||
        a.address.includes(search) ||
        (a.dpt ?? '').toLowerCase().includes(search.toLowerCase())
    );

    const map = new Map<string, Map<string, KnxAddress[]>>();
    for (const addr of filtered) {
      if (!map.has(addr.category)) map.set(addr.category, new Map());
      const sub = map.get(addr.category)!;
      if (!sub.has(addr.subcategory)) sub.set(addr.subcategory, []);
      sub.get(addr.subcategory)!.push(addr);
    }
    return map;
  }, [addresses, search]);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 200, tolerance: 8 } }),
  );

  const handleGroupDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    setGroups((prev) => {
      const oldIndex = prev.indexOf(active.id as string);
      const newIndex = prev.indexOf(over.id as string);
      return arrayMove(prev, oldIndex, newIndex);
    });
  };

  const sortedGroups = useMemo(() => [...groups].sort((a, b) => a.localeCompare(b)), [groups]);

  const addGroup = () => {
    const name = newGroup.trim();
    if (!name || groups.includes(name)) return;
    setGroups((prev) => [...prev, name]);
    setNewGroup('');
  };

  const removeGroup = (name: string) => {
    setGroups((prev) => prev.filter((g) => g !== name));
    // Clear group assignment from items that used this group
    setSelected((prev) => {
      const next = new Map(prev);
      next.forEach((item, addr) => {
        if (item.group === name) next.set(addr, { ...item, group: undefined });
      });
      return next;
    });
  };

  const toggle = (addr: KnxAddress) => {
    setSelected((prev) => {
      const next = new Map(prev);
      if (next.has(addr.address)) {
        next.delete(addr.address);
      } else {
        next.set(addr.address, {
          address: addr.address,
          label: addr.name,
          group: groups[0],
          order: next.size,
        });
      }
      return next;
    });
  };

  const setLabel = (address: string, label: string) => {
    setSelected((prev) => {
      const next = new Map(prev);
      const item = next.get(address);
      if (item) next.set(address, { ...item, label });
      return next;
    });
  };

  const setGroup = (address: string, group: string) => {
    setSelected((prev) => {
      const next = new Map(prev);
      const item = next.get(address);
      if (item) next.set(address, { ...item, group: group || undefined });
      return next;
    });
  };

  const handleSave = () => {
    const items = Array.from(selected.values()).map((item, i) => ({ ...item, order: i }));
    onSave({ groups, items });
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  return (
    <div className="flex flex-col h-full">
      {/* Toolbar */}
      <div className="flex items-center gap-3 px-4 py-3 bg-slate-900 border-b border-slate-700">
        <input
          type="text"
          placeholder="Suche..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="flex-1 bg-slate-800 border border-slate-600 rounded-lg px-3 py-2
                     text-white placeholder-slate-500 text-sm focus:outline-none focus:border-brand-500"
          style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
        />
        <span className="text-slate-500 text-sm whitespace-nowrap">{selected.size} ausgewählt</span>
        <button
          onClick={handleSave}
          className={`px-5 py-2 rounded-lg font-semibold text-sm transition-colors min-w-[100px]
            ${saved ? 'bg-green-600 text-white' : 'bg-brand-600 hover:bg-brand-500 text-white'}`}
        >
          {saved ? '✓ Gespeichert' : 'Speichern'}
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-6">

        {/* Group management */}
        <section>
          <h2 className="text-brand-500 font-bold text-sm uppercase tracking-wider mb-3">Gruppen</h2>
          {groups.length === 0 ? (
            <p className="text-slate-500 text-sm mb-3">Noch keine Gruppen. Erstelle zuerst eine Gruppe.</p>
          ) : (
            <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleGroupDragEnd}>
              <SortableContext items={groups} strategy={verticalListSortingStrategy}>
                <div className="flex flex-col gap-2 mb-3">
                  {groups.map((g) => (
                    <SortableGroupRow key={g} name={g} onRemove={() => removeGroup(g)} />
                  ))}
                </div>
              </SortableContext>
            </DndContext>
          )}
          <div className="flex gap-2">
            <input
              type="text"
              value={newGroup}
              onChange={(e) => setNewGroup(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && addGroup()}
              placeholder="Gruppenname..."
              className="flex-1 bg-slate-800 border border-slate-600 rounded-lg px-3 py-2
                         text-white placeholder-slate-500 text-sm focus:outline-none focus:border-brand-500"
              style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
            />
            <button
              onClick={addGroup}
              className="px-4 py-2 bg-slate-700 hover:bg-slate-600 text-white rounded-lg text-sm font-medium"
            >
              + Hinzufügen
            </button>
          </div>
        </section>

        {/* Address list */}
        <section>
          <h2 className="text-brand-500 font-bold text-sm uppercase tracking-wider mb-3">Adressen</h2>
          <div className="space-y-6">
            {Array.from(xmlGrouped.entries()).map(([category, subcategories]) => (
              <div key={category}>
                <h3 className="text-slate-300 font-semibold text-sm mb-2">{category}</h3>
                <div className="space-y-4">
                  {Array.from(subcategories.entries()).map(([sub, addrs]) => (
                    <div key={sub}>
                      {sub && <p className="text-slate-500 text-xs mb-1 pl-1">{sub}</p>}
                      <div className="space-y-1">
                        {addrs.map((addr) => {
                          const isSelected = selected.has(addr.address);
                          const item = selected.get(addr.address);
                          return (
                            <div
                              key={addr.address}
                              className={`rounded-xl border transition-colors
                                ${isSelected ? 'bg-slate-800 border-brand-600' : 'bg-slate-900 border-slate-700'}`}
                            >
                              <div
                                className="flex items-center gap-3 p-3 cursor-pointer"
                                onClick={() => toggle(addr)}
                              >
                                <div className={`w-6 h-6 rounded-md border-2 flex items-center justify-center shrink-0
                                  ${isSelected ? 'bg-brand-600 border-brand-600' : 'border-slate-600'}`}>
                                  {isSelected && <span className="text-white text-xs">✓</span>}
                                </div>
                                <div className="flex-1 min-w-0">
                                  <p className="text-white text-sm font-medium truncate">{addr.name}</p>
                                  <p className="text-slate-500 text-xs font-mono">{addr.address}</p>
                                </div>
                                {addr.dpt && (
                                  <span className="text-xs font-mono bg-slate-700 text-slate-300 px-2 py-0.5 rounded shrink-0">
                                    {addr.dpt}
                                  </span>
                                )}
                              </div>

                              {isSelected && (
                                <div className="px-3 pb-3 pt-0 pl-3 sm:pl-12 flex flex-col sm:flex-row gap-2">
                                  <input
                                    type="text"
                                    value={item?.label ?? addr.name}
                                    placeholder="Anzeigename..."
                                    onClick={(e) => e.stopPropagation()}
                                    onChange={(e) => setLabel(addr.address, e.target.value)}
                                    className="flex-1 bg-slate-700 border border-slate-600 rounded-lg px-3 py-1.5
                                               text-white text-sm focus:outline-none focus:border-brand-500"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                  />
                                  <select
                                    value={item?.group ?? ''}
                                    onClick={(e) => e.stopPropagation()}
                                    onChange={(e) => setGroup(addr.address, e.target.value)}
                                    className="w-full sm:w-36 bg-slate-700 border border-slate-600 rounded-lg px-3 py-1.5
                                               text-white text-sm focus:outline-none focus:border-brand-500"
                                  >
                                    <option value="">Keine Gruppe</option>
                                    {sortedGroups.map((g) => (
                                      <option key={g} value={g}>{g}</option>
                                    ))}
                                  </select>
                                </div>
                              )}
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}

            {xmlGrouped.size === 0 && (
              <p className="text-center text-slate-500 py-12">Keine Adressen gefunden.</p>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}
