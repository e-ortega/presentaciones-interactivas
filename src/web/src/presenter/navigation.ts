// Navigation is always by (slide, step), never by slide alone (D-26). Today every slide has
// one step; when animations arrive a slide will have several, and nothing here changes.

/** Where the presentation is: which slide, and which build step of that slide. */
export type Position = {
  slideIndex: number
  stepIndex: number
}

/** What navigation needs to know about a slide. Slides are in deck order. */
export type SlideSteps = {
  stepCount: number
}

export const firstPosition: Position = { slideIndex: 0, stepIndex: 0 }

export function lastPosition(slides: SlideSteps[]): Position {
  const slideIndex = slides.length - 1
  return { slideIndex, stepIndex: slides[slideIndex].stepCount - 1 }
}

/** The position one step forward, or null at the end of the deck. */
export function nextPosition(slides: SlideSteps[], current: Position): Position | null {
  if (current.stepIndex < slides[current.slideIndex].stepCount - 1) {
    return { slideIndex: current.slideIndex, stepIndex: current.stepIndex + 1 }
  }
  if (current.slideIndex < slides.length - 1) {
    return { slideIndex: current.slideIndex + 1, stepIndex: 0 }
  }
  return null
}

/** The position one step back, or null at the start of the deck. */
export function previousPosition(slides: SlideSteps[], current: Position): Position | null {
  if (current.stepIndex > 0) {
    return { slideIndex: current.slideIndex, stepIndex: current.stepIndex - 1 }
  }
  if (current.slideIndex > 0) {
    const slideIndex = current.slideIndex - 1
    return { slideIndex, stepIndex: slides[slideIndex].stepCount - 1 }
  }
  return null
}

/**
 * Where a key press leads, like the keys of PowerPoint and of presentation remotes.
 * Returns null when the key is not a navigation key; at either end of the deck the
 * position stays where it is.
 */
export function positionForKey(key: string, slides: SlideSteps[], current: Position): Position | null {
  switch (key) {
    case 'ArrowRight':
    case 'ArrowDown':
    case 'PageDown':
    case ' ':
      return nextPosition(slides, current) ?? current
    case 'ArrowLeft':
    case 'ArrowUp':
    case 'PageUp':
      return previousPosition(slides, current) ?? current
    case 'Home':
      return firstPosition
    case 'End':
      return lastPosition(slides)
    default:
      return null
  }
}
