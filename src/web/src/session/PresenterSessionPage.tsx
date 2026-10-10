import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/http'
import { accessQueryKey } from '../api/queryClient'
import { createDisplayLink } from '../api/join'
import { formatSessionCode, getSession } from '../api/sessions'
import type { SessionDetail } from '../api/types'
import { Presenter } from '../presenter/Presenter'
import presenterStyles from '../presenter/Presenter.module.css'
import { Message } from '../presenter/PresenterPage'
import { useLiveSession } from '../realtime/useLiveSession'
import { Lobby } from './Lobby'
import styles from './PresenterSessionPage.module.css'

/** The teacher's window of a live session: the lobby first, then the presenter view. */
export function PresenterSessionPage() {
  const { t } = useTranslation()
  const { sessionId = '' } = useParams()
  // The slides and speaker notes, over HTTP. They do not change during the session.
  const details = useQuery({
    queryKey: ['session', sessionId],
    queryFn: () => getSession(sessionId),
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  })

  if (details.data === undefined) {
    if (details.isPending) {
      return <Message text={t('session.loading')} />
    }
    const notFound = details.error instanceof ApiError && details.error.status === 404
    return <Message text={t(notFound ? 'session.notFound' : 'session.loadFailed')} />
  }

  return <PresenterSession details={details.data} />
}

function PresenterSession({ details }: { details: SessionDetail }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const live = useLiveSession(details.id, 'Presenter')
  const [actionFailed, setActionFailed] = useState(false)
  const [displayLinkCopied, setDisplayLinkCopied] = useState(false)

  // The hub refuses a presenter only when the teacher cookie is gone, typically because it
  // expired with this tab open. Checking access again brings the access key form back.
  const refused = live.connection === 'refused'
  useEffect(() => {
    if (refused) {
      void queryClient.invalidateQueries({ queryKey: accessQueryKey })
    }
  }, [refused, queryClient])

  /** Runs something the teacher asked for and says so if the server could not do it. */
  function run(action: Promise<void>, then?: () => void) {
    action
      .then(() => {
        setActionFailed(false)
        then?.()
      })
      .catch(() => setActionFailed(true))
  }

  if (refused) {
    return <Message text={t('session.refused')} />
  }

  // Nothing is shown until the server has said how the session stands.
  const session = live.session
  if (session === null) {
    return <Message text={t('session.connecting')} />
  }

  if (session.status === 'Ended') {
    return <Message text={t('session.ended')} />
  }

  const notices = (
    <div className={styles.notices}>
      {live.connection === 'reconnecting' && <p role="status">{t('session.reconnecting')}</p>}
      {actionFailed && <p role="alert">{t('session.actionFailed')}</p>}
    </div>
  )
  const attendance = session.attendance ?? { connectedCount: 0, attendees: [] }
  const code = session.code ?? details.code

  /** The window to share on a video call or drag to the projector. Same browser, so it just opens. */
  function openDisplay() {
    window.open(`/display/${details.id}`, `display-${details.id}`)
  }

  /** For a projector on another computer: a link that works once, for two minutes. */
  function copyDisplayLink() {
    run(
      createDisplayLink(details.id).then((link) => navigator.clipboard.writeText(link)),
      () => setDisplayLinkCopied(true),
    )
  }

  function endSession(confirmation: string, then?: () => void) {
    if (window.confirm(confirmation)) {
      run(live.endSession(), then)
    }
  }

  if (session.status === 'Lobby') {
    return (
      <>
        {notices}
        <Lobby
          title={session.title}
          code={code}
          attendance={attendance}
          onStart={() => run(live.startSession())}
          onRemove={(attendanceId) => {
            const name = attendance.attendees.find((attendee) => attendee.attendanceId === attendanceId)?.displayName
            if (window.confirm(t('lobby.removeConfirm', { name }))) {
              run(live.removeParticipant(attendanceId))
            }
          }}
          onCancel={() => endSession(t('lobby.cancelConfirm'), () => void navigate('/admin'))}
          onOpenDisplay={openDisplay}
          onCopyDisplayLink={copyDisplayLink}
          displayLinkCopied={displayLinkCopied}
        />
      </>
    )
  }

  return (
    <>
      {notices}
      <Presenter
        presentationId={details.presentationId}
        slides={details.slides}
        position={{ slideIndex: session.slideIndex, stepIndex: session.stepIndex }}
        onNavigate={(position) => run(live.goTo(position))}
      >
        {/* Always in sight, so the teacher can read it out to whoever arrives late. */}
        <p className={styles.code}>
          <span className={styles.codeLabel}>{t('session.code')}</span> {formatSessionCode(code)}
        </p>
        <p className={styles.connected}>{t('session.connected', { count: attendance.connectedCount })}</p>
        <button className={presenterStyles.option} type="button" onClick={openDisplay}>
          {t('session.openDisplay')}
        </button>
        <button className={presenterStyles.option} type="button" onClick={() => endSession(t('session.endConfirm'))}>
          {t('session.end')}
        </button>
      </Presenter>
    </>
  )
}
