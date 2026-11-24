
import { useEffect, useState } from 'react'
import './App.css'

import { getContent } from './services/contentService'
import type { ContentBundle } from './types/content'

function App() {
    const [content, setContent] = useState<ContentBundle | null>(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)

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

    const selectedRegion =
        content?.regions.find(
            region => region.name === 'Northern Mindanao'
        ) ?? content?.regions[0]

    const regionPlaces = content?.places.filter(
        place =>
            place.region === selectedRegion?.name
    ) ?? []

    const featuredPlaces = regionPlaces
        .filter(place => place.isFeatured)
        .sort((a, b) => a.featuredOrder - b.featuredOrder)

    const enabledCategories = content?.categories
        .filter(category => category.isEnabled)
        .sort((a, b) => a.displayOrder - b.displayOrder) ?? []

    return (
        <div className="app">
            <header className="topbar">
                <div className="brand">
                    <span className="brand-icon">L</span>
                    <span>LugarLang</span>
                </div>

                <nav>
                    <a className="active" href="#">Discover</a>
                    <a href="#">Map</a>
                    <a href="#">About</a>
                </nav>

                <button className="account-button">Sign in</button>
            </header>

            <main className="main-content">
                <section className="welcome">
                    <p className="eyebrow">EXPLORE THE PHILIPPINES</p>
                    <h1>Discover places worth finding.</h1>
                    <p>
                        Explore destinations, local businesses, and ways to get around.
                    </p>
                </section>

                {loading && (
                    <p className="content-message">
                        Loading LugarLang content...
                    </p>
                )}

                {error && (
                    <div className="content-message error-message">
                        <p>We couldn't load the latest LugarLang content.</p>
                        <p>{error}</p>
                    </div>
                )}

                {content && (
                    <>
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
                                <p className="eyebrow">YOUR SELECTED REGION</p>
                                <h2>{selectedRegion?.name ?? 'Explore the Philippines'}</h2>
                                <p>
                                    Discover places, experiences, and local services.
                                </p>
                                <button className="primary-button">
                                    Explore region
                                </button>
                            </div>
                        </section>

                        <section className="explore-section">
                            <div className="section-heading">
                                <div>
                                    <h2>Explore</h2>
                                    <p>Find something interesting around the region.</p>
                                </div>
                            </div>

                            <div className="category-grid">
                                {enabledCategories.map(category => {
                                    const placeCount = regionPlaces.filter(
                                        place => place.categoryId === category.id
                                    ).length

                                    return (
                                        <article
                                            className="category-card"
                                            key={category.id}
                                        >
                                            <span>{category.icon}</span>
                                            <h3>{category.name}</h3>
                                            <p>
                                                {placeCount} {placeCount === 1 ? 'place' : 'places'} available
                                            </p>
                                        </article>
                                    )
                                })}
                            </div>
                        </section>

                        <section className="explore-section">
                            <div className="section-heading">
                                <div>
                                    <h2>Featured Places</h2>
                                    <p>Discover some of the region's highlighted destinations.</p>
                                </div>
                            </div>

                            {featuredPlaces.length > 0 ? (
                                <div className="category-grid">
                                    {featuredPlaces.map(place => {
                                        const photo = place.photos?.[0] || place.imageUrl

                                        return (
                                            <article
                                                className="category-card"
                                                key={place.id}
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
                                <p>No featured places are available in this region yet.</p>
                            )}
                        </section>
                    </>
                )}

                <section className="ride-section">
                    <h2>Ride Hailing</h2>
                    <p>Find available transportation services in your area.</p>
                </section>
            </main>
        </div>
    )
}

export default App
