import { NavLink } from 'react-router-dom';

interface OrbitalModuleButtonProps {
  to: string;
  icon: React.ReactNode;
  label: string;
}

export function OrbitalModuleButton({
  to,
  icon,
  label,
}: OrbitalModuleButtonProps) {
  return (
    <NavLink
      to={to}
      end={to === '/'}
      className="group flex flex-col items-center justify-center w-32 h-32 rounded-2xl bg-white/95 backdrop-blur-sm border border-gray-200/80 shadow-md transition-all duration-300 hover:scale-105 hover:shadow-xl hover:border-blue-300/60 relative"
    >
      <span className="text-blue-600 transition-transform duration-300 group-hover:scale-110">
        {icon}
      </span>
      <span className="mt-2 font-semibold text-sm text-gray-700 group-hover:text-blue-700 transition-colors">
        {label}
      </span>
    </NavLink>
  );
}