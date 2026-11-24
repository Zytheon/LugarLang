
export interface Category {
    id: string;
    name: string;
    icon: string;
    displayOrder: number;
    isEnabled: boolean;
}

export interface PlaceContacts {
    facebook: string | null;
    instagram: string | null;
    whatsApp: string | null;
    phoneNumber: string | null;
}

export interface Place {
    id: string;
    name: string;
    category: string;
    categoryId: string;
    region: string;
    province: string;
    city: string;
    latitude: number;
    longitude: number;
    imageUrl: string | null;
    isFeatured: boolean;
    featuredOrder: number;
    photos: string[];
    contacts: PlaceContacts;
    description: string;
}

export interface Region {
    name: string;
    photo: string;
}

export interface ContentBundle {
    categories: Category[];
    places: Place[];
    regions: Region[];
}

export interface ContentVersion {
    version: string;
    updatedAtUtc: string;
}
