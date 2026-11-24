
import {
    useEffect,
    useMemo,
    useRef,
    useState,
    type PointerEvent as ReactPointerEvent,
} from 'react'

import LugarMap from '../components/LugarMap'
import type { Place } from '../types/content'
import type {
    RouteJourney,
    RoutePlanResponse,
} from '../types/routing'
import { planRoutes } from '../services/contentService'

interface MapPageProps {
    places: Place[]
    onSelectPlace: (place: Place) => void
}

interface PanelPosition {
    right: number
    top: number
}

interface DragState {
    pointerId: number
    startX: number
    startY: number
    startRight: number
    startTop: number
    moved: boolean
}

interface RoutePlannerPanelProps {
    routablePlaces: Place[]
    originPlaceId: string
    destinationPlaceId: string
    setOriginPlaceId: (id: string) => void
    setDestinationPlaceId: (id: string) => void
    originPlace: Place | null
    destinationPlace: Place | null
    isPlanning: boolean
    routeError: string
    routeResponse: RoutePlanResponse | null
    journeys: RouteJourney[]
    selectedJourneyIndex: number
    setSelectedJourneyIndex: (index: number) => void
    onFindRoutes: () => void
    onClearResults: () => void
}

function formatDistance(meters: number): string {
    if (meters >= 1000) {
        return `${(meters / 1000).toFixed(2)} km`
    }

    return `${Math.round(meters)} m`
}

function JourneyMapIcon() {
    return (
        <svg
            viewBox="0 0 48 48"
            xmlns="http://www.w3.org/2000/svg"
            aria-hidden="true"
        >
            <path
                d="M5 12 L17 7 L31 12 L43 7 V36 L31 41 L17 36 L5 41 Z"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinejoin="round"
            />

            <path
                d="M17 7 V36 M31 12 V41"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                opacity="0.65"
            />

            <path
                d="M11 29 C16 23 20 31 25 25"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeDasharray="2 3"
                strokeLinecap="round"
            />

            <path
                d="M32 9 C27 9 24 13 24 17 C24 23 32 29 32 29 C32 29 40 23 40 17 C40 13 37 9 32 9 Z"
                fill="#FFFFFF"
                stroke="currentColor"
                strokeWidth="2"
            />

            <circle
                cx="32"
                cy="17"
                r="2.5"
                fill="#486F91"
            />
        </svg>
    )
}

function RoutePlannerPanel({
    routablePlaces,
    originPlaceId,
    destinationPlaceId,
    setOriginPlaceId,
    setDestinationPlaceId,
    originPlace,
    destinationPlace,
    isPlanning,
    routeError,
    routeResponse,
    journeys,
    selectedJourneyIndex,
    setSelectedJourneyIndex,
    onFindRoutes,
    onClearResults,
}: RoutePlannerPanelProps) {
    const hostRef = useRef<HTMLDivElement>(null)
    const panelRef = useRef<HTMLDivElement>(null)

    const dragRef = useRef<DragState | null>(null)
    const suppressClickRef = useRef(false)

    const [isMinimized, setIsMinimized] = useState(false)
    const [position, setPosition] = useState<PanelPosition | null>(null)

    // Initialize the panel using the expanded panel's width.
    // Both states retain the same top-left coordinates.
    useEffect(() => {
        const panel = panelRef.current

        if (panel) {
            setPosition({
                right: 16,
                top: 100,
            })
        }

        function handlePointerMove(event: PointerEvent) {
            const drag = dragRef.current

            if (!drag || event.pointerId !== drag.pointerId) {
                return
            }

            const deltaX = event.clientX - drag.startX
            const deltaY = event.clientY - drag.startY

            if (Math.abs(deltaX) > 3 || Math.abs(deltaY) > 3) {
                drag.moved = true
            }

            const currentPanel = panelRef.current

            if (!currentPanel) return

            const maxRight = Math.max(
                0,
                window.innerWidth - currentPanel.offsetWidth
            )

            const maxTop = Math.max(
                0,
                window.innerHeight - currentPanel.offsetHeight
            )

            // Moving right decreases the right offset.
            const right = Math.min(
                maxRight,
                Math.max(0, drag.startRight - deltaX)
            )

            const top = Math.min(
                maxTop,
                Math.max(0, drag.startTop + deltaY)
            )

            setPosition({ right, top })
        }

        function handlePointerUp(event: PointerEvent) {
            const drag = dragRef.current

            if (!drag || event.pointerId !== drag.pointerId) {
                return
            }

            if (drag.moved) {
                suppressClickRef.current = true
            }

            dragRef.current = null
        }

        window.addEventListener('pointermove', handlePointerMove)
        window.addEventListener('pointerup', handlePointerUp)
        window.addEventListener('pointercancel', handlePointerUp)

        return () => {
            window.removeEventListener('pointermove', handlePointerMove)
            window.removeEventListener('pointerup', handlePointerUp)
            window.removeEventListener('pointercancel', handlePointerUp)
        }
    }, [])

    function startDragging(
        event: ReactPointerEvent<HTMLDivElement | HTMLButtonElement>
    ) {
        if (event.button !== 0) return

        const host = hostRef.current
        const panel = panelRef.current

        if (!host || !panel) return

        event.preventDefault()

        const hostRect = host.getBoundingClientRect()
        const panelRect = panel.getBoundingClientRect()

        const right = hostRect.right - panelRect.right
        const top = panelRect.top - hostRect.top

        setPosition({ right, top })

        dragRef.current = {
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            startRight: right,
            startTop: top,
            moved: false,
        }
    }

    function handleMinimizedClick() {
        if (suppressClickRef.current) {
            suppressClickRef.current = false
            return
        }

        setIsMinimized(false)
    }

    const panelStyle = {
        right: `${position?.right ?? 16}px`,
        top: `${position?.top ?? 100}px`,
        left: 'auto',
    }

    return (
        <div
            className="route-planner-host"
            ref={hostRef}
        >
            <div
                ref={panelRef}
                className={`route-planner-floating ${isMinimized ? 'minimized' : 'expanded'}`}
                style={panelStyle}
            >
                {isMinimized ? (
                    <button
                        className="route-planner-minimized-logo"
                        type="button"
                        aria-label="Restore journey planner"
                        title="Open journey planner"
                        onPointerDown={startDragging}
                        onClick={handleMinimizedClick}
                    >
                        <JourneyMapIcon />
                    </button>
                ) : (
                    <>
                        <div
                            className="route-planner-floating-header"
                            onPointerDown={startDragging}
                        >
                            <button
                                type="button"
                                className="route-planner-window-button"
                                title="Minimize planner"
                                aria-label="Minimize planner"
                                onPointerDown={event => {
                                    event.stopPropagation()
                                }}
                                onClick={() => setIsMinimized(true)}
                            >
                                <svg
                                    viewBox="0 0 24 24"
                                    width="24"
                                    height="24"
                                    fill="none"
                                    stroke="currentColor"
                                    strokeWidth="2"
                                    strokeLinecap="round"
                                    aria-hidden="true"
                                >
                                    <path d="M5 12h14" />
                                </svg>
                            </button>

                            <div className="route-planner-brand">
                                <div>
                                    <p className="eyebrow">
                                        PUBLIC TRANSPORT ROUTING
                                    </p>
                                    <h2>Plan your journey</h2>
                                </div>
                            </div>
                        </div>

                        <div className="route-planner-content">
                            <p className="route-planner-description">
                                Find jeepney routes and walking connections
                                between two places.
                            </p>

                            <div className="route-planner-controls">
                                <label className="route-planner-field">
                                    <span>Starting point</span>

                                    <select
                                        value={originPlaceId}
                                        onChange={event =>
                                            setOriginPlaceId(event.target.value)
                                        }
                                    >
                                        <option value="">
                                            Choose starting point
                                        </option>

                                        {routablePlaces.map(place => (
                                            <option
                                                key={place.id}
                                                value={place.id}
                                            >
                                                {place.name}
                                                {place.city ? ` — ${place.city}` : ''}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <label className="route-planner-field">
                                    <span>Destination</span>

                                    <select
                                        value={destinationPlaceId}
                                        onChange={event =>
                                            setDestinationPlaceId(event.target.value)
                                        }
                                    >
                                        <option value="">
                                            Choose destination
                                        </option>

                                        {routablePlaces.map(place => (
                                            <option
                                                key={place.id}
                                                value={place.id}
                                            >
                                                {place.name}
                                                {place.city ? ` — ${place.city}` : ''}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <button
                                    className="route-find-button"
                                    onClick={onFindRoutes}
                                    disabled={
                                        isPlanning ||
                                        !originPlace ||
                                        !destinationPlace ||
                                        originPlace.id === destinationPlace.id
                                    }
                                >
                                    {isPlanning
                                        ? 'Finding routes...'
                                        : 'Find routes'}
                                </button>
                            </div>

                            <div className="route-planner-settings">
                                <span>Maximum walking connection: 500 m</span>
                                <span>
                                    {routablePlaces.length} places available
                                </span>
                            </div>

                            {routeError && (
                                <div className="route-error">
                                    {routeError}
                                </div>
                            )}

                            {routeResponse && (
                                <div className="route-results">
                                    <div className="route-results-heading">
                                        <div>
                                            <h3>Available journeys</h3>
                                            <p>
                                                {routeResponse.journeyCount}{' '}
                                                {routeResponse.journeyCount === 1
                                                    ? 'journey'
                                                    : 'journeys'} found
                                            </p>
                                        </div>

                                        <button
                                            className="route-clear-button"
                                            onClick={onClearResults}
                                        >
                                            Clear
                                        </button>
                                    </div>

                                    {journeys.length > 0 ? (
                                        <div className="route-journey-list">
                                            {journeys.map((journey, index) => {
                                                const isSelected =
                                                    selectedJourneyIndex === index

                                                return (
                                                    <button
                                                        key={journey.journeyNumber}
                                                        className={`route-journey-card ${isSelected ? 'selected' : ''}`}
                                                        onClick={() =>
                                                            setSelectedJourneyIndex(index)
                                                        }
                                                        aria-pressed={isSelected}
                                                    >
                                                        <div className="route-journey-card-top">
                                                            <strong>
                                                                Journey {journey.journeyNumber}
                                                            </strong>

                                                            {isSelected && (
                                                                <span>Selected</span>
                                                            )}
                                                        </div>

                                                        <div className="route-journey-summary">
                                                            {journey.numberOfRides}{' '}
                                                            {journey.numberOfRides === 1
                                                                ? 'ride'
                                                                : 'rides'}
                                                            {' · '}
                                                            {journey.numberOfTransfers}{' '}
                                                            {journey.numberOfTransfers === 1
                                                                ? 'transfer'
                                                                : 'transfers'}
                                                        </div>

                                                        <div className="route-journey-distances">
                                                            <span>
                                                                Walk:{' '}
                                                                {formatDistance(
                                                                    journey.totalWalkingDistanceMeters
                                                                )}
                                                            </span>

                                                            <span>
                                                                Ride:{' '}
                                                                {formatDistance(
                                                                    journey.totalRideDistanceMeters
                                                                )}
                                                            </span>
                                                        </div>

                                                        <div className="route-journey-total">
                                                            Total distance:{' '}
                                                            {formatDistance(
                                                                journey.totalTravelDistanceMeters
                                                            )}
                                                        </div>
                                                    </button>
                                                )
                                            })}
                                        </div>
                                    ) : (
                                        <div className="route-no-results">
                                            No journeys were found between these
                                            places. Try another starting point
                                            or destination.
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    </>
                )}
            </div>
        </div>
    )
}

function MapPage({ places, onSelectPlace }: MapPageProps) {
    const [searchTerm, setSearchTerm] = useState('')
    const [selectedCategory, setSelectedCategory] = useState('all')
    const [focusedPlaceId, setFocusedPlaceId] = useState<string | null>(null)

    const [originPlaceId, setOriginPlaceId] = useState('')
    const [destinationPlaceId, setDestinationPlaceId] = useState('')

    const [routeResponse, setRouteResponse] =
        useState<RoutePlanResponse | null>(null)

    const [selectedJourneyIndex, setSelectedJourneyIndex] = useState(0)

    const [isPlanning, setIsPlanning] = useState(false)
    const [routeError, setRouteError] = useState('')

    const categories = useMemo(() => {
        return Array.from(
            new Set(
                places
                    .map(place => place.category)
                    .filter(Boolean)
            )
        ).sort()
    }, [places])

    const routablePlaces = useMemo(() => {
        return places.filter(place =>
            Number.isFinite(place.latitude) &&
            Number.isFinite(place.longitude) &&
            place.latitude >= -90 &&
            place.latitude <= 90 &&
            place.longitude >= -180 &&
            place.longitude <= 180
        )
    }, [places])

    const filteredPlaces = useMemo(() => {
        const search = searchTerm.trim().toLowerCase()

        return places.filter(place => {
            const matchesCategory =
                selectedCategory === 'all' ||
                place.category === selectedCategory

            const matchesSearch =
                !search ||
                [
                    place.name,
                    place.category,
                    place.city,
                    place.province,
                    place.region,
                ].some(value =>
                    value?.toLowerCase().includes(search)
                )

            return matchesCategory && matchesSearch
        })
    }, [places, searchTerm, selectedCategory])

    const focusedPlace =
        filteredPlaces.find(
            place => place.id === focusedPlaceId
        ) ?? null

    const originPlace =
        routablePlaces.find(
            place => place.id === originPlaceId
        ) ?? null

    const destinationPlace =
        routablePlaces.find(
            place => place.id === destinationPlaceId
        ) ?? null

    const journeys = routeResponse?.journeys ?? []

    const selectedJourney: RouteJourney | null =
        journeys[selectedJourneyIndex] ?? null

    function clearRouteResults() {
        setRouteResponse(null)
        setSelectedJourneyIndex(0)
        setRouteError('')
    }

    function clearFilters() {
        setSearchTerm('')
        setSelectedCategory('all')
        setFocusedPlaceId(null)
    }

    async function handleFindRoutes() {
        if (!originPlace || !destinationPlace) {
            setRouteError(
                'Please select both a starting point and a destination.'
            )
            return
        }

        if (originPlace.id === destinationPlace.id) {
            setRouteError(
                'Your starting point and destination must be different.'
            )
            return
        }

        setIsPlanning(true)
        setRouteError('')
        setRouteResponse(null)
        setSelectedJourneyIndex(0)

        try {
            const result = await planRoutes({
                fromLatitude: originPlace.latitude,
                fromLongitude: originPlace.longitude,
                toLatitude: destinationPlace.latitude,
                toLongitude: destinationPlace.longitude,
                maximumWalkingDistanceMeters: 500,
            })

            setRouteResponse(result)
            setSelectedJourneyIndex(0)
        } catch (error) {
            setRouteError(
                error instanceof Error
                    ? error.message
                    : 'Unable to plan this journey.'
            )
        } finally {
            setIsPlanning(false)
        }
    }

    return (
        <div className="map-page">
            <section className="map-page-heading">
                <div>
                    <p className="eyebrow">EXPLORE THE REGION</p>
                    <h1>Discover places on the map.</h1>
                    <p>
                        Find destinations and local businesses across LugarLang.
                    </p>
                </div>

                <div className="map-place-count">
                    <strong>{filteredPlaces.length}</strong>
                    <span>
                        {filteredPlaces.length === 1 ? 'place' : 'places'} found
                    </span>
                </div>
            </section>

            <section className="map-toolbar">
                <input
                    type="search"
                    className="map-search"
                    placeholder="Search places, cities, or categories..."
                    value={searchTerm}
                    onChange={event => {
                        setSearchTerm(event.target.value)
                        setFocusedPlaceId(null)
                    }}
                    aria-label="Search places"
                />

                <select
                    className="map-category-filter"
                    value={selectedCategory}
                    onChange={event => {
                        setSelectedCategory(event.target.value)
                        setFocusedPlaceId(null)
                    }}
                    aria-label="Filter places by category"
                >
                    <option value="all">All categories</option>

                    {categories.map(category => (
                        <option key={category} value={category}>
                            {category}
                        </option>
                    ))}
                </select>
            </section>

            <section className="map-workspace">
                <aside className="map-results-panel">
                    <div className="map-results-heading">
                        <div>
                            <h2>Places</h2>
                            <p>Select a place to locate it on the map.</p>
                        </div>

                        <span>{filteredPlaces.length}</span>
                    </div>

                    {filteredPlaces.length > 0 ? (
                        <div className="map-results-list">
                            {filteredPlaces.map(place => {
                                const photo =
                                    place.photos?.[0] || place.imageUrl

                                const isFocused =
                                    focusedPlaceId === place.id

                                return (
                                    <article
                                        key={place.id}
                                        className={`map-result-card ${isFocused ? 'focused' : ''}`}
                                    >
                                        <button
                                            className="map-result-main"
                                            onClick={() =>
                                                setFocusedPlaceId(place.id)
                                            }
                                            aria-pressed={isFocused}
                                        >
                                            {photo ? (
                                                <img
                                                    className="map-result-photo"
                                                    src={photo}
                                                    alt={place.name}
                                                />
                                            ) : (
                                                <div className="map-result-photo-placeholder">
                                                    📍
                                                </div>
                                            )}

                                            <div className="map-result-information">
                                                <h3>{place.name}</h3>
                                                <p className="map-result-category">
                                                    {place.category}
                                                </p>
                                                <p className="map-result-location">
                                                    {[place.city, place.province]
                                                        .filter(Boolean)
                                                        .join(', ') ||
                                                        'Location not specified'}
                                                </p>
                                            </div>
                                        </button>

                                        <div className="map-result-actions">
                                            <button
                                                className="map-result-details"
                                                onClick={() =>
                                                    onSelectPlace(place)
                                                }
                                            >
                                                View details →
                                            </button>

                                            {isFocused && (
                                                <span className="map-result-selected">
                                                    Selected
                                                </span>
                                            )}
                                        </div>
                                    </article>
                                )
                            })}
                        </div>
                    ) : (
                        <div className="map-results-empty">
                            <span>📍</span>
                            <h3>No places found</h3>
                            <p>Try another search or category.</p>

                            <button
                                className="primary-button"
                                onClick={clearFilters}
                            >
                                Clear filters
                            </button>
                        </div>
                    )}
                </aside>

                <div className="map-display-panel">
                    {filteredPlaces.length > 0 ? (
                        <LugarMap
                            places={filteredPlaces}
                            focusedPlace={focusedPlace}
                            onFocusPlace={place =>
                                setFocusedPlaceId(place.id)
                            }
                            onSelectPlace={onSelectPlace}
                            selectedJourney={selectedJourney}
                            originPlace={originPlace}
                            destinationPlace={destinationPlace}
                        />
                    ) : (
                        <div className="map-empty-state">
                            <span>📍</span>
                            <h2>No places to display</h2>
                            <p>
                                Clear your filters to see places on the map.
                            </p>

                            <button
                                className="primary-button"
                                onClick={clearFilters}
                            >
                                Clear filters
                            </button>
                        </div>
                    )}
                </div>
            </section>

            <RoutePlannerPanel
                routablePlaces={routablePlaces}
                originPlaceId={originPlaceId}
                destinationPlaceId={destinationPlaceId}
                setOriginPlaceId={id => {
                    setOriginPlaceId(id)
                    clearRouteResults()
                }}
                setDestinationPlaceId={id => {
                    setDestinationPlaceId(id)
                    clearRouteResults()
                }}
                originPlace={originPlace}
                destinationPlace={destinationPlace}
                isPlanning={isPlanning}
                routeError={routeError}
                routeResponse={routeResponse}
                journeys={journeys}
                selectedJourneyIndex={selectedJourneyIndex}
                setSelectedJourneyIndex={setSelectedJourneyIndex}
                onFindRoutes={handleFindRoutes}
                onClearResults={clearRouteResults}
            />

            <p className="map-attribution-note">
                Map data © OpenStreetMap contributors.
            </p>
        </div>
    )
}

export default MapPage
