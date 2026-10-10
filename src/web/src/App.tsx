import { useTranslation } from 'react-i18next'
import { Link, Navigate, Route, Routes } from 'react-router'
import styles from './App.module.css'
import { AccessGate } from './components/AccessGate'
import { HealthStatus } from './components/HealthStatus'
import { PresentationsPage } from './components/PresentationsPage'
import { PresenterPage } from './presenter/PresenterPage'
import { PresenterSessionPage } from './session/PresenterSessionPage'

/** Which screen each address shows. Everything under /admin needs the access key. */
function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/admin" replace />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route
        path="/admin/presentations/:presentationId/present"
        element={
          <AccessGate>
            <PresenterPage />
          </AccessGate>
        }
      />
      <Route
        path="/present/:sessionId"
        element={
          <AccessGate>
            <PresenterSessionPage />
          </AccessGate>
        }
      />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

function AdminPage() {
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

function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <main className={styles.page}>
      <h1 className={styles.title}>{t('notFound.title')}</h1>
      <p>
        <Link className={styles.link} to="/admin">
          {t('notFound.back')}
        </Link>
      </p>
    </main>
  )
}

export default App
