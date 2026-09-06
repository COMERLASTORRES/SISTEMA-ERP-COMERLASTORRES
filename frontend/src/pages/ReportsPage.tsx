import { useState } from 'react';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { SalesReportContent } from './SalesReportPage';
import { SalesByCustomerReportContent } from './SalesByCustomerReportPage';
import { SalesByVendorReportContent } from './SalesByVendorReportPage';
import { SalesByProductReportContent } from './SalesByProductReportPage';
import { PurchasesBySupplierReportContent } from './PurchasesBySupplierReportPage';
import { PurchasesByProductReportContent } from './PurchasesByProductReportPage';
import { InventoryBelowMinimumReportContent } from './InventoryBelowMinimumReportPage';
import { ValuedStockReportContent } from './ValuedStockReportPage';
import { ProductsWithoutMovementReportContent } from './ProductsWithoutMovementReportPage';
import { KardexReportContent } from './KardexReportPage';
import { CashFlowReportContent } from './CashFlowReportPage';
import { TopCustomersReportContent } from './TopCustomersReportPage';
import { ProductMarginReportContent } from './ProductMarginReportPage';

type ReportTab = 'period' | 'customer' | 'vendor' | 'product' | 'purchases-supplier' | 'purchases-product' | 'inventory-below-minimum' | 'inventory-valued-stock' | 'inventory-without-movement' | 'inventory-kardex' | 'cash-flow' | 'top-customers' | 'product-margin';

const TABS: { id: ReportTab; label: string }[] = [
  { id: 'period', label: 'Por período' },
  { id: 'customer', label: 'Por cliente' },
  { id: 'vendor', label: 'Por vendedor' },
  { id: 'product', label: 'Por producto' },
  { id: 'purchases-supplier', label: 'Compras por proveedor' },
  { id: 'purchases-product', label: 'Compras por producto' },
  { id: 'inventory-below-minimum', label: 'Stock por debajo del mínimo' },
  { id: 'inventory-valued-stock', label: 'Stock valorizado' },
  { id: 'inventory-without-movement', label: 'Productos sin movimiento' },
  { id: 'inventory-kardex', label: 'Kardex' },
  { id: 'cash-flow', label: 'Flujo de Caja' },
  { id: 'top-customers', label: 'Clientes Más Compradores' },
  { id: 'product-margin', label: 'Margen por Producto' },
];

export function ReportsPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.ReportsView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <ReportsContent />
    </RequirePermission>
  );
}

function ReportsContent() {
  const [activeTab, setActiveTab] = useState<ReportTab>('period');

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-gray-800">Reportes de Ventas</h1>

      {/* Pestañas */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm ${
                activeTab === tab.id
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Contenido de la pestaña activa */}
      <div className="mt-4">
        {activeTab === 'period' && <SalesReportContent />}
        {activeTab === 'customer' && <SalesByCustomerReportContent />}
        {activeTab === 'vendor' && <SalesByVendorReportContent />}
        {activeTab === 'product' && <SalesByProductReportContent />}
        {activeTab === 'purchases-supplier' && <PurchasesBySupplierReportContent />}
        {activeTab === 'purchases-product' && <PurchasesByProductReportContent />}
        {activeTab === 'inventory-below-minimum' && <InventoryBelowMinimumReportContent />}
        {activeTab === 'inventory-valued-stock' && <ValuedStockReportContent />}
        {activeTab === 'inventory-without-movement' && <ProductsWithoutMovementReportContent />}
        {activeTab === 'inventory-kardex' && <KardexReportContent />}
        {activeTab === 'cash-flow' && <CashFlowReportContent />}
        {activeTab === 'top-customers' && <TopCustomersReportContent />}
        {activeTab === 'product-margin' && <ProductMarginReportContent />}
      </div>
    </div>
  );
}