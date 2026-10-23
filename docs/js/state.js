// ЭЛЕМЕНТЫ DOM
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const loadFileBtn = document.getElementById('loadFileBtn');
const translateBtn = document.getElementById('translateBtn');
const viewTreeBtn = document.getElementById('viewTreeBtn');
const downloadBtn = document.getElementById('downloadBtn');

// Стартовый экран
const welcomeScreen = document.getElementById('welcomeScreen');
const welcomeLoadFileBtn = document.getElementById('welcomeLoadFileBtn');
const welcomeChooseLangBtn = document.getElementById('welcomeChooseLangBtn');
const mainApp = document.getElementById('mainApp');

// Дерево разбора
let lastTreeData = null;

// Уведомления
const progressToast = document.getElementById('progressToast');
const successToast = document.getElementById('successToast');
const unknownLangToast = document.getElementById('unknownLangToast');

// Модалка сохранения файла
const modal = document.getElementById('filenameModal');
const filenameInput = document.getElementById('filenameInput');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelModalBtn = document.getElementById('cancelModalBtn');
const submitFilenameBtn = document.getElementById('submitFilenameBtn');


const LANGUAGES = {
    kt:     { name: 'Kotlin', flag: '💎', ext: '.kt',    theme: 'kotlin', aliases: ['.kt', '.kts']         },
    sharp:  { name: 'CSharp',  flag: '☕', ext: '.sharp', theme: 'sharp',  aliases: ['.cs', '.sharp']       },
    python: { name: 'Python', flag: '🐍', ext: '.py',    theme: 'python', aliases: ['.py', '.pyw', '.pyi'] },
};

const AVAILABLE_SOURCE_LANGS = ['kt', 'sharp', 'python'];

let sourceLanguage = null;
const targetLanguage = 'cpp';
const TARGET_EXT = '.cpp';

// DOM модалки языков
const languageModal = document.getElementById('languageModal');
const languageList = document.getElementById('languageList');

const leftLangBtn = document.getElementById('leftLangBtn');
const leftLangName = document.getElementById('leftLangName');

// Содержимое файла, ожидающее выбора языка
let pendingFileContent = null;


const THEMES = {
    kotlin: { title: 'Kotlin++' },
    sharp:  { title: 'CSharp++'  },
    python: { title: 'Python++' },
};

const appTitle = document.getElementById('appTitle');

let isManualResizing = false;