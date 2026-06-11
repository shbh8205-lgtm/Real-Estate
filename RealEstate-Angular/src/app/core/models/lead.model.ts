export interface Lead {
  id: number;
  propertyId: number;
  propertyTitle: string;
  clientId: number;
  clientName: string;
  clientEmail: string;
  clientPhone: string;
  message: string;
  createdAt: string;
}

export interface CreateLeadRequest {
  propertyId: number;
  message: string;
}

export interface Dashboard {
  totalProperties: number;
  averagePrice: number;
  minPrice: number;
  maxPrice: number;
  totalLeads: number;
  recentLeads: Lead[];
}
