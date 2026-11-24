
import { useEffect, useState } from 'react'
import './App.css'

import { getContent } from './services/contentService'
import type { ContentBundle, Place } from './types/content'

import PlaceDetails from './pages/PlaceDetails'
import MapPage from './pages/MapPage'

type CurrentPage = 'discover' | 'map'

function App() {
    const [content, setContent] = useState<ContentBundle | null>(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)

    const [selectedCategoryId, setSelectedCategoryId] =
        useState<string | null>(null)

    const [selectedPlace, setSelectedPlace] =
        useState<Place | null>(null)

    const [currentPage, setCurrentPage] =
        useState<CurrentPage>('discover')

    // Load content from the LugarLang API
    useEffect(() => {
        async function loadContent() {
            try {
                setLoading(true)
                setError(null)

                const data = await getContent()
                setContent(data)
            } catch (err) {
                console.error('LugarLang content error:', err)

                setError(
                    err instanceof Error
                        ? err.message
                        : 'Unable to load LugarLang content.'
                )
            } finally {
                setLoading(false)
            }
        }

        loadContent()
    }, [])

    // Get the selected region
    const selectedRegion =
        content?.regions.find(
            region => region.name === 'Northern Mindanao'
        ) ?? content?.regions[0]

    // Get places belonging to the selected region
    const regionPlaces = content?.places.filter(
        place => place.region === selectedRegion?.name
    ) ?? []

    // Get featured places
    const featuredPlaces = regionPlaces
        .filter(place => place.isFeatured)
        .sort((a, b) => a.featuredOrder - b.featuredOrder)

    // Get enabled categories
    const enabledCategories = content?.categories
        .filter(category => category.isEnabled)
        .sort((a, b) => a.displayOrder - b.displayOrder) ?? []

    // Identify the selected category
    const selectedCategory = enabledCategories.find(
        category => category.id === selectedCategoryId
    )

    // Filter places according to the selected category
    const displayedPlaces = selectedCategoryId
        ? regionPlaces.filter(
            place => place.categoryId === selectedCategoryId
        )
        : featuredPlaces

    // Open a place's details
    function openPlace(place: Place) {
        setSelectedPlace(place)
    }

    // Return to Discover
    function openDiscover() {
        setSelectedPlace(null)
        setCurrentPage('discover')
    }

    // Open Map
    function openMap() {
        setSelectedPlace(null)
        setCurrentPage('map')
    }

    return (
        <div className="app">

            {/* Header */}
            <header className="topbar">
                <div className="brand">
                    <span className="brand-icon">L</span>
                    <span>LugarLang</span>
                </div>

                <nav>
                    <a
                        className={
                            currentPage === 'discover' && !selectedPlace
                                ? 'active'
                                : ''
                        }
                        href="#discover"
                        onClick={event => {
                            event.preventDefault()
                            openDiscover()
                        }}
                    >
                        Discover
                    </a>

                    <a
                        className={
                            currentPage === 'map' && !selectedPlace
                                ? 'active'
                                : ''
                        }
                        href="#map"
                        onClick={event => {
                            event.preventDefault()
                            openMap()
                        }}
                    >
                        Map
                    </a>

                    <a href="#about">About</a>
                </nav>

                <button className="account-button">
                    Sign in
                </button>
            </header>

            <main className="main-content">

                {/* Place Details */}
                {selectedPlace ? (

                    <PlaceDetails
                        place={selectedPlace}
                        onBack={() => setSelectedPlace(null)}
                    />

                ) : currentPage === 'map' ? (

                    /* Map Page */
                    <MapPage
                        places={regionPlaces}
                        onSelectPlace={openPlace}
                    />

                ) : (

                    /* Discover Page */
                    <>

                        {/* Welcome */}
                        <section className="welcome">
                            <p className="eyebrow">
                                EXPLORE THE PHILIPPINES
                            </p>

                            <h1>
                                Discover places worth finding.
                            </h1>

                            <p>
                                Explore destinations, local businesses, and ways to get around.
                            </p>
                        </section>

                        {/* Loading state */}
                        {loading && (
                            <p className="content-message">
                                Loading LugarLang content...
                            </p>
                        )}

                        {/* Error state */}
                        {error && (
                            <div className="content-message error-message">
                                <p>
                                    We couldn't load the latest LugarLang content.
                                </p>

                                <p>{error}</p>
                            </div>
                        )}

                        {/* Loaded content */}
                        {content && (
                            <>

                                {/* Region banner */}
                                <section
                                    className="region-banner"
                                    style={
                                        selectedRegion?.photo
                                            ? {
                                                backgroundImage: `linear-gradient(90deg, rgba(32, 55, 70, 0.88), rgba(32, 55, 70, 0.25)), url("${selectedRegion.photo}")`,
                                                backgroundSize: 'cover',
                                                backgroundPosition: 'center',
                                            }
                                            : undefined
                                    }
                                >
                                    <div>
                                        <p className="eyebrow">
                                            YOUR SELECTED REGION
                                        </p>

                                        <h2>
                                            {selectedRegion?.name ?? 'Explore the Philippines'}
                                        </h2>

                                        <p>
                                            Discover places, experiences, and local services.
                                        </p>

                                        <button className="primary-button">
                                            Explore region
                                        </button>
                                    </div>
                                </section>

                                {/* Categories */}
                                <section className="explore-section">
                                    <div className="section-heading">
                                        <div>
                                            <h2>Explore</h2>

                                            <p>
                                                Find something interesting around the region.
                                            </p>
                                        </div>
                                    </div>

                                    <div className="category-grid">
                                        {enabledCategories.map(category => {
                                            const placeCount = regionPlaces.filter(
                                                place => place.categoryId === category.id
                                            ).length

                                            const isSelected =
                                                selectedCategoryId === category.id

                                            return (
                                                <article
                                                    className={`category-card ${isSelected ? 'selected' : ''}`}
                                                    key={category.id}
                                                    role="button"
                                                    tabIndex={0}
                                                    aria-pressed={isSelected}
                                                    onClick={() =>
                                                        setSelectedCategoryId(
                                                            isSelected ? null : category.id
                                                        )
                                                    }
                                                    onKeyDown={event => {
                                                        if (
                                                            event.key === 'Enter' ||
                                                            event.key === ' '
                                                        ) {
                                                            event.preventDefault()

                                                            setSelectedCategoryId(
                                                                isSelected ? null : category.id
                                                            )
                                                        }
                                                    }}
                                                >
                                                    <span>{category.icon}</span>

                                                    <h3>{category.name}</h3>

                                                    <p>
                                                        {placeCount}{' '}
                                                        {placeCount === 1 ? 'place' : 'places'} available
                                                    </p>
                                                </article>
                                            )
                                        })}
                                    </div>
                                </section>

                                {/* Places */}
                                <section className="explore-section">
                                    <div className="section-heading">
                                        <div>
                                            <h2>
                                                {selectedCategory
                                                    ? `${selectedCategory.name} Places`
                                                    : 'Featured Places'}
                                            </h2>

                                            <p>
                                                {selectedCategory
                                                    ? `Explore ${selectedCategory.name.toLowerCase()} in ${selectedRegion?.name}.`
                                                    : "Discover some of the region's highlighted destinations."}
                                            </p>

                                            {selectedCategory && (
                                                <button
                                                    className="primary-button"
                                                    onClick={() => setSelectedCategoryId(null)}
                                                >
                                                    Show all featured places
                                                </button>
                                            )}
                                        </div>
                                    </div>

                                    {displayedPlaces.length > 0 ? (
                                        <div className="category-grid">
                                            {displayedPlaces.map(place => {
                                                const photo =
                                                    place.photos?.[0] || place.imageUrl

                                                return (
                                                    <article
                                                        className="category-card"
                                                        key={place.id}
                                                        role="button"
                                                        tabIndex={0}
                                                        onClick={() => openPlace(place)}
                                                        onKeyDown={event => {
                                                            if (
                                                                event.key === 'Enter' ||
                                                                event.key === ' '
                                                            ) {
                                                                event.preventDefault()
                                                                openPlace(place)
                                                            }
                                                        }}
                                                    >
                                                        {photo ? (
                                                            <img
                                                                src={photo}
                                                                alt={place.name}
                                                                style={{
                                                                    width: '100%',
                                                                    height: '150px',
                                                                    objectFit: 'cover',
                                                                    borderRadius: '8px',
                                                                }}
                                                            />
                                                        ) : (
                                                            <span>📍</span>
                                                        )}

                                                        <h3>{place.name}</h3>

                                                        <p>
                                                            {place.category} · {place.city}
                                                        </p>
                                                    </article>
                                                )
                                            })}
                                        </div>
                                    ) : (
                                        <p>
                                            {selectedCategory
                                                ? `No ${selectedCategory.name.toLowerCase()} places are available in this region yet.`
                                                : 'No featured places are available in this region yet.'}
                                        </p>
                                    )}
                                </section>

                            </>
                        )}

                        {/* Ride Hailing */}
                        <section className="ride-section">
                            <h2>Ride Hailing</h2>

                            <p>
                                Find available transportation services in your area.
                            </p>
                        </section>

                    </>
                )}

            </main>
        </div>
    )
}

export default App
