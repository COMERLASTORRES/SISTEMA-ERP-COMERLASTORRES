import { exportToExcel } from '../api/reports';
import type { SalesByPeriodFilters } from '../api/reports';

export function useExcelExport() {
  const downloadExcel = async (
    endpoint: () => Promise<{ data: Blob }>,
    filename: string
  ): Promise<void> => {
    try {
      const response = await endpoint();
      const blob = response.data;
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Error exporting to Excel:', error);
      throw error;
    }
  };

  return {
    exportSalesByPeriod: (filters: SalesByPeriodFilters) =>
      downloadExcel(() => exportToExcel.salesByPeriod(filters), 'ventas_por_periodo.xlsx'),
    exportSalesByCustomer: (filters: SalesByPeriodFilters) =>
      downloadExcel(() => exportToExcel.salesByCustomer(filters), 'ventas_por_cliente.xlsx'),
    exportSalesByVendor: (filters: SalesByPeriodFilters) =>
      downloadExcel(() => exportToExcel.salesByVendor(filters), 'ventas_por_vendedor.xlsx'),
    exportSalesByProduct: (filters: SalesByPeriodFilters) =>
      downloadExcel(() => exportToExcel.salesByProduct(filters), 'ventas_por_producto.xlsx'),
    exportPurchasesBySupplier: (filters: SalesByPeriodFilters) =>
      downloadExcel(() => exportToExcel.purchasesBySupplier(filters), 'compras_por_proveedor.xlsx'),
    exportPurchasesByProduct: (filters: SalesByPeriodFilters) =>
      downloadExcel(() => exportToExcel.purchasesByProduct(filters), 'compras_por_producto.xlsx'),
  };
}