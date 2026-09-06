import { useState, useEffect } from 'react';
import { Button } from '../components/ui/Button';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { api } from '../api/client';
import type { TenantSettings } from '../api/settings';

export function SettingsPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.SettingsView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <SettingsContent />
    </RequirePermission>
  );
}

export function SettingsContent() {
  const [settings, setSettings] = useState<TenantSettings>({
    name: '',
    ruc: '',
    address: '',
    phone: '',
    email: '',
    logoUrl: '',
    currencySymbol: 'S/',
    currencyCode: 'PEN',
    saleNumberPrefix: 'VEN-',
    purchaseNumberPrefix: 'PUR-',
    documentNumberPadding: 6,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const { data } = await api.get<TenantSettings>('/api/settings');
      setSettings(data);
    } catch (err: any) {
      setError('Error al cargar la configuración');
    } finally {
      setIsLoading(false);
    }
  };

  const saveSettings = async () => {
    setIsSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await api.put('/api/settings', settings);
      setSuccess(true);
    } catch (err: any) {
      setError('Error al guardar la configuración');
    } finally {
      setIsSaving(false);
    }
  };

  const handleChange = (field: keyof TenantSettings, value: string | number) => {
    setSettings((prev) => ({ ...prev, [field]: value }));
  };

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6 max-w-3xl">
      <h1 className="text-2xl font-bold text-gray-800">Configuración</h1>

      {error && <ErrorMessage message={error} />}
      {success && <div className="p-4 bg-green-50 text-green-700 rounded-lg">Guardado exitosamente</div>}

      {/* Datos de la empresa */}
      <div className="bg-white rounded-lg shadow p-6 space-y-4">
        <h2 className="text-lg font-semibold text-gray-700">Datos de la empresa</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Nombre</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.name}
              onChange={(e) => handleChange('name', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">RUC</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.ruc}
              onChange={(e) => handleChange('ruc', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1 md:col-span-2">
            <label className="text-sm font-medium text-gray-700">Dirección</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.address}
              onChange={(e) => handleChange('address', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Teléfono</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Email</label>
            <input
              type="email"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.email}
              onChange={(e) => handleChange('email', e.target.value)}
            />
          </div>
        </div>
      </div>

      {/* Configuración documental */}
      <div className="bg-white rounded-lg shadow p-6 space-y-4">
        <h2 className="text-lg font-semibold text-gray-700">Configuración documental</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Moneda (símbolo)</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.currencySymbol}
              onChange={(e) => handleChange('currencySymbol', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Código moneda (ISO)</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.currencyCode}
              onChange={(e) => handleChange('currencyCode', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Prefijo ventas</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.saleNumberPrefix}
              onChange={(e) => handleChange('saleNumberPrefix', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Prefijo compras</label>
            <input
              type="text"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.purchaseNumberPrefix}
              onChange={(e) => handleChange('purchaseNumberPrefix', e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Longitud correlativo</label>
            <input
              type="number"
              min="1"
              max="10"
              className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={settings.documentNumberPadding}
              onChange={(e) => handleChange('documentNumberPadding', parseInt(e.target.value) || 6)}
            />
          </div>
        </div>
      </div>

      <div className="flex justify-end">
        <Button onClick={saveSettings} disabled={isSaving}>
          {isSaving ? 'Guardando...' : 'Guardar cambios'}
        </Button>
      </div>
    </div>
  );
}