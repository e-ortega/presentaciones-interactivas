import { useTranslation } from 'react-i18next'
import { Link, Navigate, Route, Routes } from 'react-router'
import styles from './App.module.css'
import { AccessGate } from './components/AccessGate'
import { HealthStatus } from './components/HealthStatus'
import { JoinCodePage } from './join/JoinCodePage'
import { JoinFormPage } from './join/JoinFormPage'
import { PresentationsPage } from './components/PresentationsPage'
import { PresenterPage } from './presenter/PresenterPage'
import { DisplayPage } from './session/DisplayPage'
import { PresenterSessionPage } from './session/PresenterSessionPage'
import { StudentSessionPage } from './session/StudentSessionPage'

/**
 * Which screen each address shows. The teacher's screens (/admin and /present) need the access
 * key; the student's and the display window are let in by the server, each with its own cookie.
 */
function App() {
  return (
    <Routes>
      {/* Students: the code, then the form, then the class. */}
      <Route path="/" element={<JoinCodePage />} />
      <Route path="/join" element={<Navigate to="/" replace />} />
      <Route path="/s/:code" element={<JoinFormPage />} />
      <Route path="/session/:sessionId" element={<StudentSessionPage />} />
      {/* The window to share or project. */}
      <Route path="/display/:sessionId" element={<DisplayPage />} />
      {/* Teacher. */}
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
