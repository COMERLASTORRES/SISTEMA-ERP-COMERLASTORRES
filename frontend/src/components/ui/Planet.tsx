/** Premium Earth-like planet using real image */

interface PlanetProps {
  size?: number;
  className?: string;
}

export function Planet({ size = 480, className = '' }: PlanetProps) {
  return (
    <div
      className={`relative rounded-full overflow-hidden shadow-2xl shadow-blue-500/20 ${className}`}
      style={{ width: size, height: size }}
    >
      {/* Imagen de la Tierra */}
      <img
        src="/EARTH.webp"
        alt="Earth"
        className="w-full h-full object-cover rounded-full animate-spin-slow"
        style={{ transform: 'scale(1.3)', transformOrigin: 'center' }}
        onError={(e) => {
          const target = e.target as HTMLImageElement;
          target.style.display = 'none';
        }}
      />

      {/* Anillo orbital */}
      <div
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 rounded-full border border-blue-200/20 pointer-events-none"
        style={{ width: size + 100, height: size + 100 }}
      />

      {/* Halo circular sutil */}
      <div className="absolute -inset-8 rounded-full border border-blue-200/10" />

      {/* Iluminación radial de fondo */}
      <div
        className="absolute inset-0 rounded-full"
        style={{
          background: 'radial-gradient(circle, rgba(59,130,246,0.12) 0%, transparent 70%)',
        }}
      />

      {/* Atmósfera (borde azul pálido) */}
      <div className="absolute inset-0 rounded-full border-2 border-white/10" />
    </div>
  );
}