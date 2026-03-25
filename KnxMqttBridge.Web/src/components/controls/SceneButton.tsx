import { useState } from 'react';

interface Props {
  onActivate: () => void;
}

export function SceneButton({ onActivate }: Props) {
  const [triggered, setTriggered] = useState(false);

  const handleClick = () => {
    onActivate();
    setTriggered(true);
    setTimeout(() => setTriggered(false), 1500);
  };

  return (
    <button
      className={`
        w-full h-14 rounded-xl font-semibold text-base transition-all duration-150
        ${triggered
          ? 'bg-green-600 text-white scale-95'
          : 'bg-brand-600 hover:bg-brand-500 active:bg-brand-700 text-white'
        }
      `}
      onClick={handleClick}
    >
      {triggered ? '✓ Aktiviert' : 'Szene auslösen'}
    </button>
  );
}
