import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { sessionStepImageUrl } from '../api/join'
import { useLiveSession } from '../realtime/useLiveSession'
import styles from './StudentSessionPage.module.css'

/**
 * What a student sees during a class. Their view is tied to the teacher's (D-12): it shows
 * the slide the server says is on screen, and there is nothing here to move it.
 */
export function StudentSessionPage() {
  const { t } = useTranslation()
  const { sessionId = '' } = useParams()
  const live = useLiveSession(sessionId, 'Participant')

  if (live.connection === 'refused') {
    // Removed by the teacher, or this browser never joined (or its cookie is gone).
    const removed = live.refusal === 'Removed'
    return (
      <main className={styles.message}>
        <p role="alert">{t(removed ? 'student.removed' : 'student.notJoined')}</p>
        {!removed && (
          <Link className={styles.link} to="/">
            {t('student.joinWithCode')}
          </Link>
        )}
      </main>
    )
  }

  const session = live.session
  if (session === null) {
    return (
      <main className={styles.message}>
        <p role="status">{t('student.connecting')}</p>
      </main>
    )
  }

  if (session.status === 'Ended') {
    return (
      <main className={styles.message}>
        <h1 className={styles.heading}>{t('student.ended')}</h1>
        <p>{t('student.thanks')}</p>
      </main>
    )
  }

  const reconnecting = live.connection === 'reconnecting' && (
    <p className={styles.reconnecting} role="status">
      {t('session.reconnecting')}
    </p>
  )

  if (session.status === 'Lobby') {
    return (
      <main className={styles.message}>
        {reconnecting}
        <p className={styles.kicker}>{session.title}</p>
        <h1 className={styles.heading}>{t('student.youAreIn')}</h1>
        <p>{t('student.waiting')}</p>
      </main>
    )
  }

  return (
    <main className={styles.live}>
      {reconnecting}
      <p className={styles.classTitle}>{session.title}</p>
      <img
        className={styles.slide}
        src={sessionStepImageUrl(session.sessionId, session)}
        alt={t('presenter.slideAlt', { number: session.slideIndex + 1, total: session.stepCounts.length })}
      />
    </main>
  )
}
