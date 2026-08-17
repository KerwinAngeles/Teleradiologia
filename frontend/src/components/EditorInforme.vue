<script setup lang="ts">
import { watch, onBeforeUnmount } from 'vue'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Underline from '@tiptap/extension-underline'
import TextAlign from '@tiptap/extension-text-align'

const props = defineProps<{ modelValue: string; editable?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [valor: string] }>()

const editor = useEditor({
  content: props.modelValue,
  editable: props.editable !== false,
  extensions: [
    StarterKit.configure({ heading: { levels: [2, 3] } }),
    Underline,
    TextAlign.configure({ types: ['heading', 'paragraph'] }),
  ],
  editorProps: {
    attributes: { class: 'informe-prosa focus:outline-none' },
  },
  onUpdate: ({ editor: e }) => emit('update:modelValue', e.getHTML()),
})

// Solo se reemplaza el contenido si el cambio vino de afuera (aplicar plantilla, cargar
// borrador). Sin esta comparación, cada tecla reiniciaría el editor y se perdería el cursor.
watch(
  () => props.modelValue,
  (valor) => {
    if (editor.value && valor !== editor.value.getHTML()) {
      editor.value.commands.setContent(valor, { emitUpdate: false })
    }
  },
)

watch(
  () => props.editable,
  (valor) => editor.value?.setEditable(valor !== false),
)

onBeforeUnmount(() => editor.value?.destroy())

const acciones = [
  { id: 'bold', titulo: 'Negrita', atajo: 'Ctrl+B', activo: 'bold', cmd: (e: any) => e.chain().focus().toggleBold().run() },
  { id: 'italic', titulo: 'Cursiva', atajo: 'Ctrl+I', activo: 'italic', cmd: (e: any) => e.chain().focus().toggleItalic().run() },
  { id: 'underline', titulo: 'Subrayado', atajo: 'Ctrl+U', activo: 'underline', cmd: (e: any) => e.chain().focus().toggleUnderline().run() },
]
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col">
    <div
      v-if="editor && editable !== false"
      class="sticky top-0 z-10 flex flex-wrap items-center gap-1 border-b border-[var(--color-hairline)] bg-[var(--color-vidrio-solido)] px-4 py-2 backdrop-blur"
    >
      <button
        v-for="a in acciones"
        :key="a.id"
        type="button"
        class="boton-formato"
        :class="{ 'boton-formato-activo': editor.isActive(a.activo) }"
        :title="`${a.titulo} (${a.atajo})`"
        @click="a.cmd(editor)"
      >
        <span :class="{ 'font-bold': a.id === 'bold', italic: a.id === 'italic', underline: a.id === 'underline' }">
          {{ a.id === 'bold' ? 'B' : a.id === 'italic' ? 'I' : 'U' }}
        </span>
      </button>

      <span class="mx-1 h-5 w-px bg-[var(--color-hairline)]" />

      <button
        type="button"
        class="boton-formato !w-auto !px-2.5 text-xs"
        :class="{ 'boton-formato-activo': editor.isActive('heading', { level: 2 }) }"
        title="Título de sección"
        @click="editor.chain().focus().toggleHeading({ level: 2 }).run()"
      >
        Sección
      </button>
      <button
        type="button"
        class="boton-formato !w-auto !px-2.5 text-xs"
        :class="{ 'boton-formato-activo': editor.isActive('heading', { level: 3 }) }"
        title="Subtítulo"
        @click="editor.chain().focus().toggleHeading({ level: 3 }).run()"
      >
        Subtítulo
      </button>

      <span class="mx-1 h-5 w-px bg-[var(--color-hairline)]" />

      <button
        type="button"
        class="boton-formato"
        :class="{ 'boton-formato-activo': editor.isActive('bulletList') }"
        title="Lista con viñetas"
        @click="editor.chain().focus().toggleBulletList().run()"
      >
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0ZM3.75 12h.007v.008H3.75V12Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm-.375 5.25h.007v.008H3.75v-.008Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z" />
        </svg>
      </button>
      <button
        type="button"
        class="boton-formato"
        :class="{ 'boton-formato-activo': editor.isActive('orderedList') }"
        title="Lista numerada"
        @click="editor.chain().focus().toggleOrderedList().run()"
      >
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M8.242 5.992h12m-12 6.003H20.24m-12 5.999H20.24M4.117 7.495v-3.75H2.99m1.125 3.75H2.99m1.127 0H5.24m-1.123 7.5H2.99m1.127 0H5.24m-1.123 0-.002 3.75h1.128m-.002-3.75h.002m-1.128 3.75H2.99" />
        </svg>
      </button>

      <span class="mx-1 h-5 w-px bg-[var(--color-hairline)]" />

      <button
        v-for="alineacion in ['left', 'center', 'justify']"
        :key="alineacion"
        type="button"
        class="boton-formato"
        :class="{ 'boton-formato-activo': editor.isActive({ textAlign: alineacion }) }"
        :title="alineacion === 'left' ? 'Alinear a la izquierda' : alineacion === 'center' ? 'Centrar' : 'Justificar'"
        @click="editor.chain().focus().setTextAlign(alineacion).run()"
      >
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            :d="
              alineacion === 'left'
                ? 'M3.75 6.75h16.5M3.75 12h10.5m-10.5 5.25h16.5'
                : alineacion === 'center'
                  ? 'M3.75 6.75h16.5M6.75 12h10.5m-13.5 5.25h16.5'
                  : 'M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5'
            "
          />
        </svg>
      </button>

      <span class="mx-1 h-5 w-px bg-[var(--color-hairline)]" />

      <button type="button" class="boton-formato" title="Deshacer (Ctrl+Z)" @click="editor.chain().focus().undo().run()">
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 15 3 9m0 0 6-6M3 9h12a6 6 0 0 1 0 12h-3" />
        </svg>
      </button>
      <button type="button" class="boton-formato" title="Rehacer (Ctrl+Y)" @click="editor.chain().focus().redo().run()">
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="m15 15 6-6m0 0-6-6m6 6H9a6 6 0 0 0 0 12h3" />
        </svg>
      </button>
    </div>

    <EditorContent :editor="editor" class="min-h-0 flex-1 overflow-y-auto" />
  </div>
</template>
