import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { listPresentations } from '../api/presentations'
import type { PresentationSummary } from '../api/types'
import { PresentationRow } from './PresentationRow'
import styles from './PresentationsPage.module.css'
import { UploadForm } from './UploadForm'

function isImporting(presentation: PresentationSummary): boolean {
  return presentation.status === 'Pending' || presentation.status === 'Processing'
}

/** The teacher's decks, with the form to upload a new one. */
export function PresentationsPage() {
  const { t } = useTranslation()
  const presentations = useQuery({
    queryKey: ['presentations'],
    queryFn: listPresentations,
    // While something is still being imported, ask again every two seconds.
    refetchInterval: (query) => (query.state.data?.some(isImporting) ? 2000 : false),
  })

  return (
    <section>
      <h2 className={styles.title}>{t('presentations.title')}</h2>
      <UploadForm />

      {presentations.isPending && <p role="status">{t('presentations.loading')}</p>}
      {presentations.isError && <p role="alert">{t('presentations.loadFailed')}</p>}
      {presentations.data?.length === 0 && <p className={styles.empty}>{t('presentations.empty')}</p>}

      <ul className={styles.list}>
        {presentations.data?.map((presentation) => (
          <PresentationRow key={presentation.id} presentation={presentation} />
        ))}
      </ul>
    </section>
  )
}
