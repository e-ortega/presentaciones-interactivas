import { QRCodeSVG } from 'qrcode.react'
import { useTranslation } from 'react-i18next'
import { formatSessionCode, joinUrl } from '../api/sessions'
import buttonStyles from '../components/Button.module.css'
import type { AttendanceUpdate } from '../realtime/contracts'
import styles from './Lobby.module.css'

type LobbyProps = {
  title: string
  code: string
  attendance: AttendanceUpdate
  onStart: () => void
  onRemove: (attendanceId: string) => void
  onCancel: () => void
}

/** The teacher's waiting room: how to join, who has come in, and the button to begin. */
export function Lobby({ title, code, attendance, onStart, onRemove, onCancel }: LobbyProps) {
  const { t } = useTranslation()

  return (
    <main className={styles.page}>
      <header>
        <p className={styles.waiting}>{t('lobby.waiting')}</p>
        <h1 className={styles.title}>{title}</h1>
      </header>

      <section className={styles.join} aria-label={t('lobby.howToJoin')}>
        <div>
          <p className={styles.label}>{t('lobby.codeLabel')}</p>
          <p className={styles.code}>{formatSessionCode(code)}</p>
          <p className={styles.address}>{joinUrl(code)}</p>
        </div>
        <QRCodeSVG className={styles.qr} value={joinUrl(code)} size={176} marginSize={2} title={t('lobby.qrTitle')} />
      </section>

      <section aria-labelledby="attendance-heading">
        <h2 id="attendance-heading" className={styles.heading}>
          {t('lobby.attendance', { count: attendance.attendees.length })}
        </h2>

        {attendance.attendees.length === 0 ? (
          <p className={styles.empty}>{t('lobby.nobodyYet')}</p>
        ) : (
          <ul className={styles.list}>
            {attendance.attendees.map((attendee) => (
              <li key={attendee.attendanceId} className={styles.attendee}>
                <span>{attendee.displayName}</span>
                {!attendee.connected && <span className={styles.offline}>{t('lobby.disconnected')}</span>}
                <button
                  className={styles.remove}
                  type="button"
                  onClick={() => onRemove(attendee.attendanceId)}
                  aria-label={t('lobby.removeLabel', { name: attendee.displayName })}
                >
                  {t('lobby.remove')}
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      <footer className={styles.actions}>
        <button className={buttonStyles.button} type="button" onClick={onStart}>
          {t('lobby.start')}
        </button>
        <button className={styles.cancel} type="button" onClick={onCancel}>
          {t('lobby.cancel')}
        </button>
      </footer>
    </main>
  )
}
