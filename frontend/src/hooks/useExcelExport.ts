// exportToExcel no existe en api/reports.ts - DISABLED hasta implementar
// import { exportToExcel } from '../api/reports';
// import type { SalesByPeriodFilters } from '../api/reports'; // sin usar mientras exportToExcel esté deshabilitado

// DISABLED: downloadExcel y todos los métodos de exportación dependen de exportToExcel que no existe
// Se reactivará cuando se implemente exportToExcel en api/reports.ts
export function useExcelExport() {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const _stub = () => { /* stub para mantener la firma */ };
  void _stub;
  return {};
}