export const roles = ['FleetAdministrator', 'OperationsCoordinator', 'Mechanic', 'Driver', 'FleetOwner'] as const;
export type Role = typeof roles[number];
export interface CurrentUser {
  id: string;
  displayName: string;
  email: string;
  role: Role;
  driverId: string | null;
}
export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
}
export interface UserAccount extends CurrentUser {
  phoneNumber: string | null;
  isActive: boolean;
  version: number;
  createdAt: string;
  updatedAt: string;
}
export interface Page<T> { items: T[]; page: number; pageSize: number; totalCount: number }
export interface DriverOption { id: string; name: string; licenceNumber: string }
