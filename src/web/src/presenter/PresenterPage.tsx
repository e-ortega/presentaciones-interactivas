import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/http'
import { getPresentation } from '../api/presentations'
import { Presenter } from './Presenter'
import styles from './PresenterPage.module.css'

/** Loads the presentation named in the address and shows its presenter view. */
export function PresenterPage() {
  const { t } = useTranslation()
  const { presentationId = '' } = useParams()
  const presentation = useQuery({
    queryKey: ['presentation', presentationId],
    queryFn: () => getPresentation(presentationId),
  })

  if (presentation.isPending) {
    return <Message text={t('presenter.loading')} />
  }

  if (presentation.isError) {
    const notFound = presentation.error instanceof ApiError && presentation.error.status === 404
    return <Message text={t(notFound ? 'presenter.notFound' : 'presenter.loadFailed')} />
  }

  if (presentation.data.status !== 'Ready') {
    return <Message text={t('presenter.notReady')} />
  }

  return <Presenter presentation={presentation.data} />
}

/** A full-page notice with the way back to the list. */
function Message({ text }: { text: string }) {
  const { t } = useTranslation()

  return (
    <main className={styles.message}>
      <p>{text}</p>
      <Link to="/admin">{t('presenter.backToList')}</Link>
    </main>
  )
}
