export interface MasterRecord {
  id: string; version: number; isDeleted: boolean;
  audit: { createdBy: string; createdAt: string; updatedBy: string; updatedAt: string };
  registrationNumber?: string; model?: string; year?: number; capacity?: number; currentOdometer?: number;
  name?: string; licenceNumber?: string; contact?: string; status?: string;
  companyName?: string; contactPerson?: string; telephone?: string; email?: string; address?: string;
  linkedAccount?: { id: string; displayName: string; email: string; isActive: boolean } | null;
}
export type FieldKey = 'registrationNumber' | 'model' | 'year' | 'capacity' | 'currentOdometer' | 'name' | 'licenceNumber' | 'contact' | 'companyName' | 'contactPerson' | 'telephone' | 'email' | 'address' | 'status';
export interface Field {
  key: FieldKey; label: string; required?: boolean; maxLength?: number;
  type?: 'number' | 'email'; min?: number; max?: number; step?: number; options?: string[];
}
export interface MasterConfig {
  endpoint: 'vehicles' | 'drivers' | 'customers'; title: string; singular: string;
  nameKey: FieldKey; description: string; fields: Field[]; columns: FieldKey[]; statuses?: string[];
}
export interface Page { items: MasterRecord[]; page: number; pageSize: number; totalCount: number }
export function recordName(config: MasterConfig, record: MasterRecord) { return String(record[config.nameKey] ?? ''); }
export function label(value: string) { return value.replace(/([a-z])([A-Z])/g, '$1 $2'); }
