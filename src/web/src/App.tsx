import { useTranslation } from 'react-i18next'
import styles from './App.module.css'
import { AccessGate } from './components/AccessGate'
import { HealthStatus } from './components/HealthStatus'
import { PresentationsPage } from './components/PresentationsPage'

function App() {
  const { t } = useTranslation()

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>{t('app.name')}</h1>
        <p className={styles.tagline}>{t('app.tagline')}</p>
      </header>

      <main>
        <AccessGate>
          <PresentationsPage />
        </AccessGate>
      </main>

      <footer className={styles.footer}>
        <HealthStatus />
      </footer>
    </div>
  )
}

export default App
