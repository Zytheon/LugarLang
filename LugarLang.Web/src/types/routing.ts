
export interface RouteCoordinate {
    latitude: number
    longitude: number
}

export interface RoutePlanRequest {
    fromLatitude: number
    fromLongitude: number
    toLatitude: number
    toLongitude: number
    maximumWalkingDistanceMeters: number
}

export interface RouteWalkingSegment {
    type: 'origin' | 'transfer' | 'destination'
    from: RouteCoordinate
    to: RouteCoordinate
    distanceMeters: number
    coordinates: RouteCoordinate[]
}

export interface RoutePlanLeg {
    routeId: string
    routeName: string
    routeCode: string
    directionName: string
    directionSummary: string
    fromWalkingDistanceMeters: number
    toWalkingDistanceMeters: number
    rideDistanceMeters: number
    boardingPoint: RouteCoordinate
    alightingPoint: RouteCoordinate
    routeCoordinates: RouteCoordinate[]
}

export interface RouteJourney {
    journeyNumber: number
    numberOfRides: number
    numberOfTransfers: number
    totalWalkingDistanceMeters: number
    totalRideDistanceMeters: number
    totalTravelDistanceMeters: number
    legs: RoutePlanLeg[]
    walkingSegments: RouteWalkingSegment[]
}

export interface RoutePlanResponse {
    journeyCount: number
    journeys: RouteJourney[]
}
