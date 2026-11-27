<script setup lang="ts">
interface Props {
  modelValue: string
  label?: string
}

defineProps<Props>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const open = ref(false)
const search = ref('')
const containerEl = ref<HTMLElement | null>(null)
const searchEl = ref<HTMLInputElement | null>(null)

interface EmojiEntry {
  emoji: string
  keywords: string
}

interface EmojiCategory {
  name: string
  emojis: EmojiEntry[]
}

const categories: EmojiCategory[] = [
  {
    name: '👨‍💻 Software & Dev',
    emojis: [
      { emoji: '💻', keywords: 'laptop computer code dev' },
      { emoji: '🖥️', keywords: 'desktop monitor screen server' },
      { emoji: '📱', keywords: 'mobile phone app ios android' },
      { emoji: '⌨️', keywords: 'keyboard typing input' },
      { emoji: '🖱️', keywords: 'mouse click cursor' },
      { emoji: '🖨️', keywords: 'printer output' },
      { emoji: '🧑‍💻', keywords: 'developer coder programmer engineer' },
      { emoji: '👾', keywords: 'bug alien game glitch' },
      { emoji: '🐛', keywords: 'bug defect issue error' },
      { emoji: '🔧', keywords: 'wrench fix tool repair maintenance' },
      { emoji: '🛠️', keywords: 'tools build repair devtools' },
      { emoji: '⚙️', keywords: 'gear settings cog config' },
      { emoji: '🔩', keywords: 'bolt screw nut assembly' },
      { emoji: '🔨', keywords: 'hammer build compile' },
      { emoji: '⛏️', keywords: 'pickaxe mine extract parse' },
      { emoji: '🪛', keywords: 'screwdriver fix configure' },
      { emoji: '🧰', keywords: 'toolbox toolkit sdk' },
      { emoji: '📦', keywords: 'package box deploy npm module' },
      { emoji: '🗃️', keywords: 'card file box archive storage' },
      { emoji: '🗄️', keywords: 'file cabinet database storage' },
      { emoji: '💾', keywords: 'floppy disk save storage' },
      { emoji: '💿', keywords: 'cd disc image iso' },
      { emoji: '🖲️', keywords: 'trackball input device' },
      { emoji: '📀', keywords: 'dvd disc backup' },
      { emoji: '🔌', keywords: 'plug connect power electric' },
      { emoji: '🔋', keywords: 'battery energy power' },
      { emoji: '📡', keywords: 'satellite antenna network signal api' },
      { emoji: '🛜', keywords: 'wifi wireless network' },
      { emoji: '🌐', keywords: 'globe network internet web' },
      { emoji: '🔗', keywords: 'link chain url hyperlink' },
      { emoji: '🔐', keywords: 'lock key secure auth closed' },
      { emoji: '🔒', keywords: 'lock secure closed private' },
      { emoji: '🔓', keywords: 'unlock open public access' },
      { emoji: '🛡️', keywords: 'shield security protect firewall' },
      { emoji: '🔑', keywords: 'key access token auth credential' },
      { emoji: '🗝️', keywords: 'key old auth secret token' },
      { emoji: '🧱', keywords: 'brick block wall architecture layer' },
      { emoji: '🏗️', keywords: 'construction scaffold build infra' },
      { emoji: '🚀', keywords: 'rocket launch deploy ship fast' },
      { emoji: '🛸', keywords: 'ufo spacecraft cloud serverless' },
      { emoji: '☁️', keywords: 'cloud aws azure gcp hosting' },
      { emoji: '⚡', keywords: 'lightning fast performance zap' },
      { emoji: '🔥', keywords: 'fire hot trending popular' },
      { emoji: '💥', keywords: 'boom crash explosion error' },
      { emoji: '✨', keywords: 'sparkles new feature magic' },
      { emoji: '🌀', keywords: 'cyclone spinning loading async' },
      { emoji: '♻️', keywords: 'recycle refactor reuse green' },
      { emoji: '🔄', keywords: 'arrows refresh sync loop cycle' },
      { emoji: '🔃', keywords: 'arrows reload refresh rotate' },
      { emoji: '⏱️', keywords: 'stopwatch timing benchmark performance' },
      { emoji: '⏰', keywords: 'alarm clock cron schedule job' },
      { emoji: '📅', keywords: 'calendar date schedule sprint' },
      { emoji: '🗓️', keywords: 'spiral calendar planning roadmap' },
      { emoji: '📋', keywords: 'clipboard list tasks backlog' },
      { emoji: '📝', keywords: 'memo note docs comment' },
      { emoji: '📄', keywords: 'document file page report' },
      { emoji: '📂', keywords: 'folder directory open file' },
      { emoji: '📁', keywords: 'folder closed directory' },
      { emoji: '🗂️', keywords: 'index tabs files organise' },
      { emoji: '📊', keywords: 'bar chart analytics metrics dashboard' },
      { emoji: '📈', keywords: 'chart up growth trend scaling' },
      { emoji: '📉', keywords: 'chart down decrease regression' },
      { emoji: '🔍', keywords: 'search magnify find query' },
      { emoji: '🔎', keywords: 'right magnify zoom debug inspect' },
      { emoji: '🧪', keywords: 'test tube experiment testing lab' },
      { emoji: '🧫', keywords: 'petri dish culture test env' },
      { emoji: '🧬', keywords: 'dna genetics algorithm data' },
      { emoji: '🔬', keywords: 'microscope science inspect detail' },
      { emoji: '🔭', keywords: 'telescope research explore observe' },
      { emoji: '🧮', keywords: 'abacus calculate compute math' },
      { emoji: '🤖', keywords: 'robot ai automation bot ml' },
      { emoji: '🧠', keywords: 'brain ai ml intelligence logic' },
      { emoji: '💡', keywords: 'lightbulb idea innovation solution' },
      { emoji: '📌', keywords: 'pin mark anchor todo' },
      { emoji: '📍', keywords: 'pin location map endpoint' },
      { emoji: '🏷️', keywords: 'label tag version release badge' },
      { emoji: '🎯', keywords: 'target goal milestone objective' },
      { emoji: '✅', keywords: 'check done complete pass green' },
      { emoji: '❌', keywords: 'cross fail error wrong red' },
      { emoji: '⚠️', keywords: 'warning caution alert deprecate' },
      { emoji: '🚦', keywords: 'traffic light status ci pipeline' },
      { emoji: '🚧', keywords: 'construction wip in progress' },
      { emoji: '🆕', keywords: 'new feature badge' },
      { emoji: '🆙', keywords: 'up upgrade update' },
      { emoji: '🔼', keywords: 'up arrow upgrade bump' },
      { emoji: '⬆️', keywords: 'up arrow major upgrade' },
      { emoji: '⬇️', keywords: 'down arrow downgrade rollback' },
      { emoji: '↩️', keywords: 'return revert rollback undo' },
      { emoji: '🔀', keywords: 'shuffle merge branch git' },
      { emoji: '📤', keywords: 'outbox push upload export' },
      { emoji: '📥', keywords: 'inbox pull download import' },
      { emoji: '📨', keywords: 'envelope incoming message event' },
      { emoji: '📬', keywords: 'mailbox message queue inbox' },
      { emoji: '💬', keywords: 'speech chat message log comment' },
      { emoji: '🗨️', keywords: 'chat bubble comment message' },
      { emoji: '📣', keywords: 'megaphone announce broadcast' },
      { emoji: '🔔', keywords: 'bell notification alert event' },
    ],
  },
  {
    name: 'Smileys & People',
    emojis: [
      { emoji: '😀', keywords: 'grinning happy smile' },
      { emoji: '😊', keywords: 'blush happy smile' },
      { emoji: '😎', keywords: 'cool sunglasses' },
      { emoji: '🤩', keywords: 'star struck excited' },
      { emoji: '🥳', keywords: 'partying celebrate' },
      { emoji: '😍', keywords: 'heart eyes love' },
      { emoji: '🤔', keywords: 'thinking' },
      { emoji: '🤓', keywords: 'nerd glasses' },
      { emoji: '👍', keywords: 'thumbs up like ok' },
      { emoji: '👋', keywords: 'wave hi hello' },
      { emoji: '🙌', keywords: 'hands celebrate' },
      { emoji: '👏', keywords: 'clap applause' },
      { emoji: '🤝', keywords: 'handshake deal' },
      { emoji: '💪', keywords: 'muscle strong' },
      { emoji: '👤', keywords: 'person user profile' },
      { emoji: '👥', keywords: 'people group team' },
      { emoji: '🧑‍💻', keywords: 'developer coder tech' },
      { emoji: '🧑‍🎨', keywords: 'artist designer creative' },
    ],
  },
  {
    name: 'Business & Finance',
    emojis: [
      { emoji: '📊', keywords: 'chart bar analytics data' },
      { emoji: '📈', keywords: 'chart up growth trend' },
      { emoji: '📉', keywords: 'chart down decrease' },
      { emoji: '💰', keywords: 'money bag cash' },
      { emoji: '💳', keywords: 'credit card payment' },
      { emoji: '🏦', keywords: 'bank money finance' },
      { emoji: '💼', keywords: 'briefcase business work' },
      { emoji: '📋', keywords: 'clipboard list tasks' },
      { emoji: '📝', keywords: 'memo note write' },
      { emoji: '📄', keywords: 'document file page' },
      { emoji: '📂', keywords: 'folder files' },
      { emoji: '🗓️', keywords: 'calendar date schedule' },
      { emoji: '⏰', keywords: 'alarm clock time' },
      { emoji: '📌', keywords: 'pin location mark' },
      { emoji: '✉️', keywords: 'email envelope mail' },
      { emoji: '🔑', keywords: 'key access lock' },
    ],
  },
  {
    name: 'Education & Ideas',
    emojis: [
      { emoji: '🎓', keywords: 'graduate school learn education' },
      { emoji: '📚', keywords: 'books study library' },
      { emoji: '📖', keywords: 'book read open' },
      { emoji: '✏️', keywords: 'pencil write edit' },
      { emoji: '🎯', keywords: 'target goal bullseye' },
      { emoji: '🏆', keywords: 'trophy award win' },
      { emoji: '🥇', keywords: 'gold medal first best' },
      { emoji: '⭐', keywords: 'star favorite' },
      { emoji: '✨', keywords: 'sparkles shine' },
      { emoji: '🔍', keywords: 'search magnify find' },
      { emoji: '💬', keywords: 'speech chat message' },
      { emoji: '💭', keywords: 'thought think idea' },
      { emoji: '🗣️', keywords: 'speak talk voice' },
      { emoji: '🌟', keywords: 'glowing star shine' },
    ],
  },
  {
    name: 'Nature',
    emojis: [
      { emoji: '🌍', keywords: 'earth globe world' },
      { emoji: '🌱', keywords: 'seedling grow plant' },
      { emoji: '🌿', keywords: 'herb leaf green' },
      { emoji: '🍃', keywords: 'leaves wind green' },
      { emoji: '🌸', keywords: 'flower blossom pink' },
      { emoji: '🌻', keywords: 'sunflower yellow' },
      { emoji: '🌊', keywords: 'wave ocean water' },
      { emoji: '🔥', keywords: 'fire hot flame' },
      { emoji: '⚡', keywords: 'lightning bolt electric' },
      { emoji: '❄️', keywords: 'snowflake cold winter' },
      { emoji: '☀️', keywords: 'sun sunny warm' },
      { emoji: '🌙', keywords: 'moon night crescent' },
      { emoji: '🌈', keywords: 'rainbow color' },
      { emoji: '🚀', keywords: 'rocket launch space fast' },
    ],
  },
  {
    name: 'Symbols',
    emojis: [
      { emoji: '✅', keywords: 'check done complete ok' },
      { emoji: '❌', keywords: 'cross no wrong' },
      { emoji: '⚠️', keywords: 'warning caution alert' },
      { emoji: 'ℹ️', keywords: 'info information' },
      { emoji: '❓', keywords: 'question help' },
      { emoji: '❗', keywords: 'exclamation important' },
      { emoji: '🔴', keywords: 'red circle' },
      { emoji: '🟡', keywords: 'yellow circle' },
      { emoji: '🟢', keywords: 'green circle' },
      { emoji: '🔵', keywords: 'blue circle' },
      { emoji: '♻️', keywords: 'recycle green environment' },
      { emoji: '🏷️', keywords: 'label tag price' },
      { emoji: '🎁', keywords: 'gift present' },
      { emoji: '🎉', keywords: 'party celebrate tada' },
      { emoji: '💯', keywords: 'hundred percent perfect' },
      { emoji: '🆕', keywords: 'new badge' },
    ],
  },
]

const filteredCategories = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return categories
  return categories
    .map(cat => ({
      name: cat.name,
      emojis: cat.emojis.filter(e => e.keywords.includes(q) || e.emoji === q),
    }))
    .filter(cat => cat.emojis.length > 0)
})

function select(emoji: string) {
  emit('update:modelValue', emoji)
  open.value = false
  search.value = ''
}

function clear() {
  emit('update:modelValue', '')
  open.value = false
  search.value = ''
}

function toggle() {
  open.value = !open.value
  if (open.value) {
    search.value = ''
    nextTick(() => searchEl.value?.focus())
  }
}

function onDocumentMousedown(e: MouseEvent) {
  if (open.value && containerEl.value && !containerEl.value.contains(e.target as Node)) {
    open.value = false
    search.value = ''
  }
}

function onDocumentKeydown(e: KeyboardEvent) {
  if (open.value && e.key === 'Escape') {
    open.value = false
    search.value = ''
  }
}

onMounted(() => {
  document.addEventListener('mousedown', onDocumentMousedown)
  document.addEventListener('keydown', onDocumentKeydown)
})
onUnmounted(() => {
  document.removeEventListener('mousedown', onDocumentMousedown)
  document.removeEventListener('keydown', onDocumentKeydown)
})
</script>

<template>
  <div ref="containerEl" class="relative">
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
      {{ label }}
    </label>
    <button
      type="button"
      class="flex items-center gap-2 px-3 py-2 border rounded-lg shadow-sm bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500 transition-colors"
      @click="toggle"
    >
      <span class="text-xl leading-none w-6 text-center">{{ modelValue || '😶' }}</span>
      <span class="text-sm text-gray-500 dark:text-gray-400">{{ modelValue ? modelValue : 'Pick emoji…' }}</span>
    </button>

    <div
      v-if="open"
      class="absolute z-50 mt-1 w-72 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg"
    >
      <div class="p-2 border-b border-gray-100 dark:border-gray-700 flex gap-2">
        <input
          ref="searchEl"
          v-model="search"
          type="text"
          placeholder="Search emojis…"
          class="flex-1 px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 dark:text-gray-100 focus:outline-none focus:ring-1 focus:ring-primary-500"
        />
        <button
          type="button"
          class="text-xs px-2 py-1 text-gray-500 dark:text-gray-400 hover:text-red-500 border border-gray-300 dark:border-gray-600 rounded-md transition-colors"
          @click="clear"
        >
          Clear
        </button>
      </div>

      <div class="overflow-y-auto max-h-64 p-2 space-y-3">
        <template v-if="filteredCategories.length > 0">
          <div v-for="cat in filteredCategories" :key="cat.name">
            <p class="text-xs font-medium text-gray-400 dark:text-gray-500 mb-1">{{ cat.name }}</p>
            <div class="grid grid-cols-8 gap-0.5">
              <button
                v-for="entry in cat.emojis"
                :key="entry.emoji"
                type="button"
                :title="entry.keywords"
                class="text-xl p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors leading-none"
                @click="select(entry.emoji)"
              >
                {{ entry.emoji }}
              </button>
            </div>
          </div>
        </template>
        <p v-else class="text-sm text-center text-gray-400 py-4">
          No emojis found
        </p>
      </div>
    </div>
  </div>
</template>
