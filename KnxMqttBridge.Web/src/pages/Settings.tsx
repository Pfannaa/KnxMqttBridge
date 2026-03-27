import { useState } from 'react';
import { Check } from 'lucide-react';
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
            <label className="text-zinc-300 text-sm font-medium">{label}</label>
            {children}
            {hint && <p className="text-zinc-500 text-xs">{hint}</p>}
        </div>
    );
}

const inputClass =
    'bg-zinc-800 border border-zinc-600 rounded-lg px-3 py-2.5 text-white text-sm ' +
    'focus:outline-none focus:border-brand-500 placeholder-zinc-500';

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

                    <section>
                        <h2 className="text-zinc-200 font-semibold text-sm mb-4">MQTT Broker</h2>
                        <div className="space-y-4">
                            <Field label="Broker Host" hint="Hostname or IP address of the MQTT broker">
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
                                hint="Port for MQTT over WebSocket (default: 9001). Must be enabled in Mosquitto."
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

                            <Field label="Username (optional)">
                                <input
                                    type="text"
                                    className={inputClass}
                                    value={form.mqttUsername}
                                    onChange={(e) => set('mqttUsername', e.target.value)}
                                    placeholder="leave empty if not needed"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                />
                            </Field>

                            <Field label="Password (optional)">
                                <input
                                    type="password"
                                    className={inputClass}
                                    value={form.mqttPassword}
                                    onChange={(e) => set('mqttPassword', e.target.value)}
                                    placeholder="leave empty if not needed"
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                />
                            </Field>

                            <Field label="Topic Prefix" hint='Default: "knx" → Topics: knx/GroupAddresses/...'>
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

                    <section>
                        <h2 className="text-zinc-200 font-semibold text-sm mb-4">KNX Configuration</h2>
                        <div className="space-y-4">
                            <Field label="Address format">
                                <select
                                    className={inputClass}
                                    value={form.addressStyle}
                                    onChange={(e) => set('addressStyle', e.target.value)}
                                    style={{ userSelect: 'auto', WebkitUserSelect: 'auto' }}
                                >
                                    <option value="ThreeLevel">Three levels (1/2/3)</option>
                                    <option value="TwoLevel">Two levels (1/2)</option>
                                </select>
                            </Field>
                        </div>
                    </section>

                    <section className="bg-zinc-800/60 rounded-xl p-4">
                        <h3 className="text-zinc-300 font-semibold text-sm mb-2">Enable Mosquitto WebSocket</h3>
                        <p className="text-zinc-400 text-xs mb-3">
                            Add the following lines to <code className="text-brand-400">mosquitto.conf</code>:
                        </p>
                        <pre className="bg-zinc-900 rounded-lg p-3 text-xs text-green-400 overflow-x-auto">
                            {`listener 9001
protocol websockets
allow_anonymous true`}
                        </pre>
                    </section>
                </div>
            </div>

            <div className="border-t border-zinc-700 p-4">
                <div className="max-w-lg mx-auto">
                    <button
                        onClick={handleSave}
                        className={`
              w-full h-12 rounded-xl font-semibold text-base transition-colors
              ${saved ? 'bg-green-600 text-white' : 'bg-brand-600 hover:bg-brand-500 text-white'}
            `}
                    >
                        {saved ? <span className="flex items-center justify-center gap-2"><Check className="w-4 h-4" />Saved</span> : 'Save settings'}
                    </button>
                    {saved && (
                        <p className="text-center text-zinc-400 text-sm mt-2">
                            MQTT connection will reconnect automatically.
                        </p>
                    )}
                </div>
            </div>
        </div>
    );
}
