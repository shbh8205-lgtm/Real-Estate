export interface Property {
  id: number;
  title: string;
  description: string;
  price: number;        // decimal בC# → number בTS
  address: string;
  createdAt: string;    // DateTime → ISO string
  tags: string;         // string רגיל, לא string[]
  // descriptionVector לא נשלח ל-Frontend
}

export interface CreatePropertyRequest {
  title: string;
  description: string;
  price: number;
  address: string;
  tags: string;
}

export interface PropertySearchParams {
  query?: string;
  minPrice?: number;
  maxPrice?: number;
}