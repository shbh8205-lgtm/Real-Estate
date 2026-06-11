// core/models/user.model.ts

export type ClientRole = 'Buyer' | 'Renter' | 'Agent';

export interface Client {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: ClientRole;
  savedProperties: number[]; // Property IDs
}

export interface Agent {
  id: string;           // Guid
  fullName: string;
  licenseNumber: string;
  averageRating: number;
}

export interface AuthResponse {
  token: string;
  client: Client;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phoneNumber: string;
  maxBudget: number;
  role: ClientRole;
}