import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/http'
import { getPresentation } from '../api/presentations'
import type { PresentationDetail } from '../api/types'
import { firstPosition } from './navigation'
import { Presenter } from './Presenter'
import presenterStyles from './Presenter.module.css'
import styles from './PresenterPage.module.css'

/** Rehearsal: loads the presentation named in the address and shows its presenter view, with no students. */
export function PresenterPage() {
  const { t } = useTranslation()
  const { presentationId = '' } = useParams()
  const presentation = useQuery({
    queryKey: ['presentation', presentationId],
    queryFn: () => getPresentation(presentationId),
    // An imported deck does not change, so there is nothing to ask again when the teacher
    // comes back to this window in the middle of a class.
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  })

  // Once the deck is on screen it stays there: a later failed request must never replace
  // the class with an error message and send the teacher back to slide 1.
  if (presentation.data === undefined) {
    if (presentation.isPending) {
      return <Message text={t('presenter.loading')} />
    }
    const notFound = presentation.error instanceof ApiError && presentation.error.status === 404
    return <Message text={t(notFound ? 'presenter.notFound' : 'presenter.loadFailed')} />
  }

  if (presentation.data.status !== 'Ready') {
    return <Message text={t('presenter.notReady')} />
  }

  return <Rehearsal presentation={presentation.data} />
}

/** The presenter view on its own: the position lives in this browser and goes nowhere else. */
function Rehearsal({ presentation }: { presentation: PresentationDetail }) {
  const { t } = useTranslation()
  const [position, setPosition] = useState(firstPosition)

  return (
    <Presenter presentationId={presentation.id} slides={presentation.slides} position={position} onNavigate={setPosition}>
      <Link className={presenterStyles.option} to="/admin">
        {t('presenter.controls.exit')}
      </Link>
    </Presenter>
  )
}

/** A full-page notice with the way back to the list. */
export function Message({ text }: { text: string }) {
  const { t } = useTranslation()

  return (
    <main className={styles.message}>
      <p>{text}</p>
      <Link to="/admin">{t('presenter.backToList')}</Link>
    </main>
  )
}
