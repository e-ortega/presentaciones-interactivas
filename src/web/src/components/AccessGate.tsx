import { useQuery, useQueryClient } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { hasAccess } from '../api/access'
import { accessQueryKey } from '../api/queryClient'
import { AccessKeyForm } from './AccessKeyForm'

type AccessGateProps = {
  /** The teacher area, shown only once the access key has been accepted. */
  children: ReactNode
}

/** Shows its children to the teacher, and the access key form to anyone else. */
export function AccessGate({ children }: AccessGateProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const access = useQuery({ queryKey: accessQueryKey, queryFn: hasAccess })

  if (access.isPending) {
    return <p role="status">{t('access.checking')}</p>
  }

  if (access.isError) {
    return <p role="alert">{t('access.unavailable')}</p>
  }

  if (!access.data) {
    // The key was accepted: remember it, which renders this component again.
    return <AccessKeyForm onAccepted={() => queryClient.setQueryData(accessQueryKey, true)} />
  }

  return children
}
