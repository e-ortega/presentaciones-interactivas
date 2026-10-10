import { useEffect, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { stepImageUrl } from '../api/presentations'
import type { SlideDetail } from '../api/types'
import { nextPosition, positionForKey, previousPosition, type Position } from './navigation'
import styles from './Presenter.module.css'
import { ScriptPanel, type ScriptMode } from './ScriptPanel'

type PresenterProps = {
  presentationId: string
  /** The slides of a presentation that finished importing, so there is at least one. */
  slides: SlideDetail[]
  /**
   * Where the presentation is. This component does not keep it: whoever uses it does.
   * Rehearsing, that is the browser; in a live class, the server.
   */
  position: Position
  /** Asked for by a button or a key. The owner of the position decides what happens. */
  onNavigate: (position: Position) => void
  /** What the bar shows after the script options: the way out, the join code, ending the class... */
  children: ReactNode
}

/**
 * The presenter view (design §4): the current slide, the script beside it and a bar of
 * controls. Everything is visible at a glance and one click or key press away.
 */
export function Presenter({ presentationId, slides, position, onNavigate, children }: PresenterProps) {
  const { t } = useTranslation()
  const [scriptVisible, setScriptVisible] = useState(true)
  const [scriptMode, setScriptMode] = useState<ScriptMode>('teleprompter')

  // Keyboard navigation. An effect connects the component to something outside React,
  // here the browser's keyboard events, and says how to disconnect when it goes away.
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.ctrlKey || event.altKey || event.metaKey || isUsingAControl(event)) {
        return
      }

      const target = positionForKey(event.key, slides, position)
      if (target === null) {
        return
      }
      event.preventDefault() // the arrows and the space bar would scroll the page
      onNavigate(target)
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
    // The listener reads these three, so it is replaced whenever one of them changes.
  }, [slides, position, onNavigate])

  const next = nextPosition(slides, position)
  const previous = previousPosition(slides, position)
  const stepCount = slides[position.slideIndex].stepCount

  return (
    <div className={scriptVisible ? styles.layout : styles.layoutWithoutScript}>
      <main className={styles.stage}>
        <img
          className={styles.slide}
          src={stepImageUrl(presentationId, position)}
          alt={t('presenter.slideAlt', { number: position.slideIndex + 1, total: slides.length })}
        />
        {/* Loads the next image ahead of time, so moving forward does not flash. */}
        {next && <img hidden src={stepImageUrl(presentationId, next)} alt="" />}
      </main>

      {scriptVisible && <ScriptPanel slides={slides} currentSlideIndex={position.slideIndex} mode={scriptMode} />}

      <nav className={styles.controls} aria-label={t('presenter.controls.label')}>
        {/*
          aria-disabled, not disabled: a disabled button that has the focus swallows key
          presses in some browsers, and the keyboard would stop working at either end.
        */}
        <button
          className={styles.navigate}
          type="button"
          aria-disabled={previous === null}
          onClick={() => previous && onNavigate(previous)}
        >
          {t('presenter.controls.previous')}
        </button>
        <button
          className={styles.navigate}
          type="button"
          aria-disabled={next === null}
          onClick={() => next && onNavigate(next)}
        >
          {t('presenter.controls.next')}
        </button>

        <p className={styles.position} aria-live="polite">
          {t('presenter.position', { number: position.slideIndex + 1, total: slides.length })}
          {stepCount > 1 && ` · ${t('presenter.step', { number: position.stepIndex + 1, total: stepCount })}`}
        </p>

        <div className={styles.options}>
          {scriptVisible && (
            <button
              className={styles.option}
              type="button"
              aria-pressed={scriptMode === 'static'}
              onClick={() => setScriptMode(scriptMode === 'static' ? 'teleprompter' : 'static')}
            >
              {t('presenter.controls.staticNotes')}
            </button>
          )}
          <button
            className={styles.option}
            type="button"
            aria-pressed={scriptVisible}
            onClick={() => setScriptVisible(!scriptVisible)}
          >
            {t('presenter.controls.script')}
          </button>
          {children}
        </div>
      </nav>
    </div>
  )
}

/**
 * Whether the key press belongs to a control rather than to the presentation: typing in a
 * field, or pressing the space bar on a focused button or link (which activates it).
 */
function isUsingAControl(event: KeyboardEvent): boolean {
  if (!(event.target instanceof HTMLElement)) {
    return false
  }
  const tag = event.target.tagName
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || event.target.isContentEditable) {
    return true
  }
  return event.key === ' ' && (tag === 'BUTTON' || tag === 'A')
}
