import { useQuery } from '@tanstack/react-query'
import { QRCodeSVG } from 'qrcode.react'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useParams } from 'react-router'
import { ApiError } from '../api/http'
import { redeemDisplayToken, sessionStepImageUrl } from '../api/join'
import { formatSessionCode, joinUrl } from '../api/sessions'
import { useLiveSession } from '../realtime/useLiveSession'
import styles from './DisplayPage.module.css'

/**
 * The window to share on a video call or to send to a projector (D-39): the waiting room
 * with the code, then the slide filling the window. No notes and no controls.
 *
 * In the teacher's own browser it just opens. On another computer it is opened with a link
 * that carries a one-time token (after the #), traded here for this window's cookie.
 */
export function DisplayPage() {
  const { t } = useTranslation()
  const { sessionId = '' } = useParams()
  const token = new URLSearchParams(useLocation().hash.slice(1)).get('token')

  // A query, so the token is sent once even when React runs effects twice in development.
  const access = useQuery({
    queryKey: ['display-access', token],
    queryFn: async () => {
      await redeemDisplayToken(token ?? '')
      return true
    },
    enabled: token !== null,
    // The default rule: a refusal (4xx) is final, a network or server failure is tried again.
    staleTime: Infinity,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  })

  // A token the server refused is spent or expired; one it could not be asked about is still good.
  const linkRefused = access.error instanceof ApiError && access.error.status < 500

  // Used or useless, the token leaves the address bar, which may end up on a shared screen.
  useEffect(() => {
    if (access.isSuccess || linkRefused) {
      window.history.replaceState(null, '', window.location.pathname)
    }
  }, [access.isSuccess, linkRefused])

  if (token !== null && access.isPending) {
    return <p className={styles.notice}>{t('display.opening')}</p>
  }

  if (token !== null && access.isError) {
    return (
      <p className={styles.notice} role="alert">
        {t(linkRefused ? 'display.linkExpired' : 'display.openFailed')}
      </p>
    )
  }

  return <Display sessionId={sessionId} />
}

function Display({ sessionId }: { sessionId: string }) {
  const { t } = useTranslation()
  const live = useLiveSession(sessionId, 'Display')

  if (live.connection === 'refused') {
    return (
      <p className={styles.notice} role="alert">
        {t('display.refused')}
      </p>
    )
  }

  const session = live.session
  if (session === null) {
    return <p className={styles.notice}>{t('display.opening')}</p>
  }

  if (session.status === 'Ended') {
    return <p className={styles.notice}>{t('session.ended')}</p>
  }

  if (session.status === 'Live') {
    return (
      <main className={styles.stage}>
        <img
          className={styles.slide}
          src={sessionStepImageUrl(session.sessionId, session)}
          alt={t('presenter.slideAlt', { number: session.slideIndex + 1, total: session.stepCounts.length })}
        />
      </main>
    )
  }

  // Waiting room. The code is the biggest thing on the screen: it is read from the back of the room.
  const attendees = session.attendance?.attendees ?? []
  const code = session.code ?? ''
  return (
    <main className={styles.lobby}>
      <header>
        <p className={styles.kicker}>{session.title}</p>
        <h1 className={styles.join}>{t('display.joinAt', { address: window.location.host })}</h1>
      </header>

      <section className={styles.how}>
        <p className={styles.code}>{formatSessionCode(code)}</p>
        <QRCodeSVG className={styles.qr} value={joinUrl(code)} size={260} marginSize={2} title={t('lobby.qrTitle')} />
      </section>

      <section aria-labelledby="display-attendance">
        <h2 id="display-attendance" className={styles.attendanceHeading}>
          {t('lobby.attendance', { count: attendees.length })}
        </h2>
        <ul className={styles.attendees}>
          {attendees.map((attendee) => (
            <li key={attendee.attendanceId}>{attendee.displayName}</li>
          ))}
        </ul>
      </section>
    </main>
  )
}
