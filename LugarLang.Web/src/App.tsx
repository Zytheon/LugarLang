
import './App.css'

function App() {
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
          <p>Explore destinations, local businesses, and ways to get around.</p>
        </section>

        <section className="region-banner">
          <div>
            <p className="eyebrow">YOUR SELECTED REGION</p>
            <h2>Northern Mindanao</h2>
            <p>Discover places, experiences, and local services.</p>
            <button className="primary-button">Explore region</button>
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
            <article className="category-card">
              <span>🍽️</span>
              <h3>Restaurants</h3>
              <p>Discover local dining spots.</p>
            </article>

            <article className="category-card">
              <span>🏢</span>
              <h3>Businesses</h3>
              <p>Find services and establishments.</p>
            </article>

            <article className="category-card">
              <span>🏞️</span>
              <h3>Attractions</h3>
              <p>Explore interesting destinations.</p>
            </article>

            <article className="category-card">
              <span>🛍️</span>
              <h3>Shopping</h3>
              <p>Find shops and local products.</p>
            </article>
          </div>
        </section>

        <section className="ride-section">
          <h2>Ride Hailing</h2>
          <p>Find available transportation services in your area.</p>
        </section>
      </main>
    </div>
  )
}

export default App
