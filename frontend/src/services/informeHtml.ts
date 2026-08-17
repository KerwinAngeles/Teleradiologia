import DOMPurify from 'dompurify'

// Los informes anteriores al editor son texto plano: se envuelven en párrafos para
// que se vean igual que los nuevos.
export function normalizarInforme(contenido: string): string {
  if (contenido.trimStart().startsWith('<')) return contenido

  return contenido
    .split(/\n{2,}/)
    .map((parrafo) => `<p>${parrafo.replace(/\n/g, '<br>')}</p>`)
    .join('')
}

/**
 * Una sola lista de etiquetas permitidas para todos los lugares que muestran un
 * informe. Con una copia por pantalla, tarde o temprano una se afloja y abre un XSS
 * en el único contenido de la aplicación que es HTML libre.
 *
 * Se sanea aunque venga de la base y lo haya escrito un usuario autenticado.
 */
export function comoHtmlSeguro(contenido: string): string {
  return DOMPurify.sanitize(normalizarInforme(contenido), {
    ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 's', 'h2', 'h3', 'ul', 'ol', 'li', 'blockquote'],
    ALLOWED_ATTR: ['style'],
  })
}
