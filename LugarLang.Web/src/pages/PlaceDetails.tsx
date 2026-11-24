
import { useState } from 'react'
import type { Place } from '../types/content'

interface PlaceDetailsProps {
    place: Place
    onBack: () => void
}

function PlaceDetails({ place, onBack }: PlaceDetailsProps) {
    const photos =
        place.photos?.length > 0
            ? place.photos
            : place.imageUrl
                ? [place.imageUrl]
                : []

    const [activePhotoIndex, setActivePhotoIndex] = useState(0)

    const contacts = place.contacts

    const whatsappNumber = contacts.whatsApp?.replace(/\D/g, '')

    const location = [
        place.city,
        place.province,
        place.region,
    ].filter(Boolean).join(', ')

    const mapUrl = `https://www.google.com/maps?q=${place.latitude},${place.longitude}`

    return (
        <div className="place-details">

            {/* Back navigation */}
            <button
                className="back-button"
                onClick={onBack}
            >
                ← Back to Discover
            </button>

            {/* Photo gallery */}
            <section className="place-gallery">
                <div className="place-main-photo">
                    {photos.length > 0 ? (
                        <img
                            src={photos[activePhotoIndex]}
                            alt={place.name}
                        />
                    ) : (
                        <div className="place-photo-placeholder">
                            <span>📍</span>
                            <p>No photos available</p>
                        </div>
                    )}
                </div>

                {photos.length > 1 && (
                    <div className="place-thumbnails">
                        {photos.map((photo, index) => (
                            <button
                                key={`${photo}-${index}`}
                                className={`place-thumbnail ${activePhotoIndex === index ? 'active' : ''
                                    }`}
                                onClick={() => setActivePhotoIndex(index)}
                                aria-label={`View photo ${index + 1}`}
                                aria-pressed={activePhotoIndex === index}
                            >
                                <img
                                    src={photo}
                                    alt={`${place.name} ${index + 1}`}
                                />
                            </button>
                        ))}
                    </div>
                )}
            </section>

            {/* Place heading */}
            <section className="place-heading">
                <div>
                    <p className="eyebrow">{place.category}</p>

                    <h1>{place.name}</h1>

                    <p className="place-location">
                        📍 {location || 'Location not specified'}
                    </p>
                </div>
            </section>

            <div className="place-information-grid">

                {/* Description */}
                <section className="place-information">
                    <h2>About this place</h2>

                    <p>
                        {place.description?.trim()
                            || 'No description is available for this place yet.'}
                    </p>
                </section>

                {/* Contact information */}
                <section className="place-information">
                    <h2>Contact Information</h2>

                    <div className="place-contact-list">

                        {contacts.facebook && (
                            <a
                                href={contacts.facebook}
                                target="_blank"
                                rel="noreferrer"
                            >
                                Facebook
                            </a>
                        )}

                        {contacts.instagram && (
                            <a
                                href={contacts.instagram}
                                target="_blank"
                                rel="noreferrer"
                            >
                                Instagram
                            </a>
                        )}

                        {whatsappNumber && (
                            <a
                                href={`https://wa.me/${whatsappNumber}`}
                                target="_blank"
                                rel="noreferrer"
                            >
                                WhatsApp
                            </a>
                        )}

                        {contacts.phoneNumber && (
                            <a href={`tel:${contacts.phoneNumber}`}>
                                {contacts.phoneNumber}
                            </a>
                        )}

                        {!contacts.facebook &&
                            !contacts.instagram &&
                            !whatsappNumber &&
                            !contacts.phoneNumber && (
                                <p>No contact information available.</p>
                            )}

                    </div>
                </section>

                {/* Location */}
                <section className="place-information">
                    <h2>Location</h2>

                    <p>
                        {location || 'Location not specified'}
                    </p>

                    <a
                        className="primary-button"
                        href={mapUrl}
                        target="_blank"
                        rel="noreferrer"
                    >
                        View on Google Maps
                    </a>

                    <p className="place-coordinates">
                        {place.latitude}, {place.longitude}
                    </p>
                </section>

            </div>
        </div>
    )
}

export default PlaceDetails
