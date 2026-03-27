import { useState } from 'react';
import type { FrontendSettings } from '../types';

interface Props {
    settings: FrontendSettings;
    onSave: (settings: FrontendSettings) => void;
}

interface FieldProps {
    label: string;
    hint?: string;
    children: React.ReactNode;
}

function Field({ label, hint, children }: FieldProps) {
    return (
        <div className="flex flex-col gap-1.5">
            <label className="text-slate-300 text-sm font-medium">{label}</label>
            {children}
            {hint && <p className="text-slate-500 text-xs">{hint}</p>}
        </div>
    );
}

const inputClass =
    'bg-slate-800 border border-slate-600 rounded-lg px-3 py-2.5 text-white text-sm ' +
    'focus:outline-none focus:border-brand-500 placeholder-slate-500';

export function Settings({ settings, onSave }: Props) {
    const [form, setForm] = useState<FrontendSettings>(settings);
    const [saved, setSaved] = useState(false);

    const set = (key: keyof FrontendSettings, value: string | number) =>
        setForm((prev) => ({ ...prev, [key]: value }));

    const handleSave = () => {
        onSave(form);
        setSaved(true);
        setTimeout(() => setSaved(false), 2500);
    };

    return (
        <div className="flex flex-col h-full">
            <div className="flex-1 overflow-y-auto p-4">
                <div className="max-w-lg mx-auto space-y-8">

                    {/* MQTT Section */}
                    <section>
                        <h2 className="text-brand-500 font-bold text-sm uppercase tracking-wider mb-4">
                            MQTT Broker
                        </h2>
                        <div className="space-y-4">
                            <Field label="Broker Host" hint="Hostname oder IP-Adresse des MQTT-Brokers">
                                <input
                                    type="text"
                                    className={inputClass}
                                    value={form.mqttBrokerHost}
                                    onChange={(e) => set('mqttBrokerHost', e.target.value)}
                                    placeholder="192.168.1.10"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                />
                            </Field>

                            <Field
                                label="WebSocket Port"
                                hint="Port für MQTT over WebSocket (Standard: 9001). Muss in Mosquitto aktiviert sein."
                            >
                                <input
                                    type="number"
                                    className={inputClass}
                                    value={form.mqttWebSocketPort}
                                    onChange={(e) => set('mqttWebSocketPort', Number(e.target.value))}
                                    placeholder="9001"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                />
                            </Field>

                            <Field label="Benutzername (optional)">
                                <input
                                    type="text"
                                    className={inputClass}
                                    value={form.mqttUsername}
                                    onChange={(e) => set('mqttUsername', e.target.value)}
                                    placeholder="leer lassen wenn nicht benötigt"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                />
                            </Field>

                            <Field label="Passwort (optional)">
                                <input
                                    type="password"
                                    className={inputClass}
                                    value={form.mqttPassword}
                                    onChange={(e) => set('mqttPassword', e.target.value)}
                                    placeholder="leer lassen wenn nicht benötigt"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                />
                            </Field>

                            <Field label="Topic Prefix" hint='Standard: "knx" → Topics: knx/GroupAddresses/...'>
                                <input
                                    type="text"
                                    className={inputClass}
                                    value={form.topicPrefix}
                                    onChange={(e) => set('topicPrefix', e.target.value)}
                                    placeholder="knx"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                />
                            </Field>
                        </div>
                    </section>

                    {/* KNX Section */}
                    <section>
                        <h2 className="text-brand-500 font-bold text-sm uppercase tracking-wider mb-4">
                            KNX Konfiguration
                        </h2>
                        <div className="space-y-4">
                            <Field label="Adressformat">
                                <select
                                    className={inputClass}
                                    value={form.addressStyle}
                                    onChange={(e) => set('addressStyle', e.target.value)}
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                >
                                    <option value="ThreeLevel">Drei Ebenen (1/2/3)</option>
                                    <option value="TwoLevel">Zwei Ebenen (1/2)</option>
                                </select>
                            </Field>
                        </div>
                    </section>

                    {/* Mosquitto hint */}
                    <section className="bg-slate-800 border border-slate-700 rounded-xl p-4">
                        <h3 className="text-slate-300 font-semibold text-sm mb-2">Mosquitto WebSocket aktivieren</h3>
                        <p className="text-slate-400 text-xs mb-3">
                            Füge folgende Zeilen zur <code className="text-brand-400">mosquitto.conf</code> hinzu:
                        </p>
                        <pre className="bg-slate-900 rounded-lg p-3 text-xs text-green-400 overflow-x-auto">
                            {`listener 9001
protocol websockets
allow_anonymous true`}
                        </pre>
                    </section>
                </div>
            </div>

            {/* Save button */}
            <div className="border-t border-slate-700 p-4">
                <div className="max-w-lg mx-auto">
                    <button
                        onClick={handleSave}
                        className={`
              w-full h-12 rounded-xl font-semibold text-base transition-colors
              ${saved
                                ? 'bg-green-600 text-white'
                                : 'bg-brand-600 hover:bg-brand-500 text-white'
                            }
            `}
                    >
                        {saved ? '✓ Gespeichert' : 'Einstellungen speichern'}
                    </button>
                    {saved && (
                        <p className="text-center text-slate-400 text-sm mt-2">
                            MQTT-Verbindung wird automatisch neu aufgebaut.
                        </p>
                    )}
                </div>
            </div>
        </div>
    );
}
