import React, { useEffect, useMemo } from 'react'

import {
    CircleMarker,
    MapContainer,
    Popup,
    Polyline,
    TileLayer,
    useMap,
} from 'react-leaflet'

import 'leaflet/dist/leaflet.css'

import type { Place } from '../types/content'

import type {
    RouteJourney,
    RouteCoordinate,
} from '../types/routing'

interface LugarMapProps {
    places: Place[]
    focusedPlace: Place | null
    onFocusPlace: (place: Place) => void
    onSelectPlace: (place: Place) => void

    selectedJourney: RouteJourney | null
    originPlace: Place | null
    destinationPlace: Place | null
}

const DEFAULT_CENTER: [number, number] = [8.4542, 124.6319]

const ROUTE_COLORS = [
    '#D97745',
    '#486F91',
    '#4E8B72',
    '#8064A2',
]

function toPosition(
    coordinate: RouteCoordinate
): [number, number] {
    return [
        coordinate.latitude,
        coordinate.longitude,
    ]
}

function MapBounds({ places }: { places: Place[] }) {
    const map = useMap()

    useEffect(() => {
        if (places.length === 1) {
            map.setView(
                [places[0].latitude, places[0].longitude],
                14
            )
        } else if (places.length > 1) {
            const bounds = places.map(
                place =>
                    [place.latitude, place.longitude] as [number, number]
            )

            map.fitBounds(bounds, {
                padding: [40, 40],
                maxZoom: 14,
            })
        } else {
            map.setView(DEFAULT_CENTER, 11)
        }
    }, [map, places])

    return null
}

function MapFocus({ place }: { place: Place | null }) {
    const map = useMap()

    useEffect(() => {
        if (place) {
            map.flyTo(
                [place.latitude, place.longitude],
                15,
                { duration: 0.8 }
            )
        }
    }, [map, place])

    return null
}

function MapRouteBounds({
    journey,
}: {
    journey: RouteJourney | null
}) {
    const map = useMap()

    useEffect(() => {
        if (!journey) return

        const positions: [number, number][] = []

        journey.legs.forEach(leg => {
            positions.push(
                ...leg.routeCoordinates.map(toPosition)
            )
        })

        journey.walkingSegments.forEach(segment => {
            positions.push(
                ...segment.coordinates.map(toPosition)
            )
        })

        if (positions.length > 1) {
            map.fitBounds(positions, {
                padding: [55, 55],
                maxZoom: 16,
            })
        }
    }, [journey, map])

    return null
}

function LugarMap({
    places,
    focusedPlace,
    onFocusPlace,
    onSelectPlace,
    selectedJourney,
    originPlace,
    destinationPlace,
}: LugarMapProps) {

    const validPlaces = useMemo(
        () => places.filter(
            place =>
                Number.isFinite(place.latitude) &&
                Number.isFinite(place.longitude) &&
                place.latitude >= -90 &&
                place.latitude <= 90 &&
                place.longitude >= -180 &&
                place.longitude <= 180
        ),
        [places]
    )

    return (
        <div className="lugar-map">
            <MapContainer
                center={DEFAULT_CENTER}
                zoom={11}
                scrollWheelZoom={true}
                className="lugar-map-canvas"
            >
                <TileLayer
                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                />

                <MapBounds places={validPlaces} />

                <MapFocus place={focusedPlace} />

                <MapRouteBounds journey={selectedJourney} />

                {/* Jeepney route lines */}
                {selectedJourney?.legs.map((leg, index) => {
                    const positions =
                        leg.routeCoordinates.map(toPosition)

                    if (positions.length < 2) return null

                    const color =
                        ROUTE_COLORS[index % ROUTE_COLORS.length]

                    return (
                        <Polyline
                            key={`${leg.routeId}-${index}`}
                            positions={positions}
                            pathOptions={{
                                color,
                                weight: 6,
                                opacity: 0.9,
                            }}
                        >
                            <Popup>
                                <div className="map-popup">
                                    <strong>{leg.routeCode}</strong>
                                    <p>{leg.routeName}</p>
                                    <p>{leg.directionSummary}</p>
                                </div>
                            </Popup>
                        </Polyline>
                    )
                })}

                {/* Walking connections */}
                {selectedJourney?.walkingSegments.map(
                    (segment, index) => {
                        const positions =
                            segment.coordinates.map(toPosition)

                        if (positions.length < 2) return null

                        return (
                            <Polyline
                                key={`walking-${index}`}
                                positions={positions}
                                pathOptions={{
                                    color: '#486F91',
                                    weight: 4,
                                    opacity: 0.9,
                                    dashArray: '8 9',
                                }}
                            >
                                <Popup>
                                    <div className="map-popup">
                                        <strong>
                                            {segment.type === 'origin'
                                                ? 'Walk to boarding point'
                                                : segment.type === 'transfer'
                                                    ? 'Walking transfer'
                                                    : 'Walk to destination'}
                                        </strong>

                                        <p>
                                            {Math.round(
                                                segment.distanceMeters
                                            )} m
                                        </p>
                                    </div>
                                </Popup>
                            </Polyline>
                        )
                    }
                )}

                {/* Starting point */}
                {selectedJourney && originPlace && (
                    <CircleMarker
                        center={[
                            originPlace.latitude,
                            originPlace.longitude,
                        ]}
                        radius={11}
                        pathOptions={{
                            color: '#FFFFFF',
                            weight: 3,
                            fillColor: '#238B57',
                            fillOpacity: 1,
                        }}
                    >
                        <Popup>
                            <strong>Starting point</strong>
                            <p>{originPlace.name}</p>
                        </Popup>
                    </CircleMarker>
                )}

                {/* Destination */}
                {selectedJourney && destinationPlace && (
                    <CircleMarker
                        center={[
                            destinationPlace.latitude,
                            destinationPlace.longitude,
                        ]}
                        radius={11}
                        pathOptions={{
                            color: '#FFFFFF',
                            weight: 3,
                            fillColor: '#C74646',
                            fillOpacity: 1,
                        }}
                    >
                        <Popup>
                            <strong>Destination</strong>
                            <p>{destinationPlace.name}</p>
                        </Popup>
                    </CircleMarker>
                )}

                {/* Boarding and alighting points */}
                {selectedJourney?.legs.map((leg, index) => {
                    const color =
                        ROUTE_COLORS[index % ROUTE_COLORS.length]

                    return (
                        <React.Fragment key={`stops-${index}`}>
                            <CircleMarker
                                center={toPosition(leg.boardingPoint)}
                                radius={7}
                                pathOptions={{
                                    color,
                                    weight: 3,
                                    fillColor: '#FFFFFF',
                                    fillOpacity: 1,
                                }}
                            >
                                <Popup>
                                    <strong>Board here</strong>
                                    <p>{leg.routeCode}</p>
                                </Popup>
                            </CircleMarker>

                            <CircleMarker
                                center={toPosition(leg.alightingPoint)}
                                radius={7}
                                pathOptions={{
                                    color,
                                    weight: 3,
                                    fillColor: '#FFFFFF',
                                    fillOpacity: 1,
                                }}
                            >
                                <Popup>
                                    <strong>Get off here</strong>
                                    <p>{leg.routeCode}</p>
                                </Popup>
                            </CircleMarker>
                        </React.Fragment>
                    )
                })}

                {/* Existing place markers */}
                {validPlaces.map(place => {
                    const isFocused =
                        focusedPlace?.id === place.id

                    return (
                        <CircleMarker
                            key={place.id}
                            center={[
                                place.latitude,
                                place.longitude,
                            ]}
                            radius={isFocused ? 12 : 9}
                            pathOptions={{
                                color: '#FFFFFF',
                                weight: isFocused ? 3 : 2,
                                fillColor: isFocused
                                    ? '#D97745'
                                    : '#486F91',
                                fillOpacity: 1,
                            }}
                            eventHandlers={{
                                click: () => onFocusPlace(place),
                            }}
                        >
                            <Popup>
                                <div className="map-popup">
                                    <strong>{place.name}</strong>

                                    <p>
                                        {place.category}
                                        {place.city
                                            ? ` · ${place.city}`
                                            : ''}
                                    </p>

                                    <button
                                        className="map-popup-button"
                                        onClick={() =>
                                            onSelectPlace(place)
                                        }
                                    >
                                        View place details
                                    </button>
                                </div>
                            </Popup>
                        </CircleMarker>
                    )
                })}
            </MapContainer>
        </div>
    )
}

export default LugarMap
