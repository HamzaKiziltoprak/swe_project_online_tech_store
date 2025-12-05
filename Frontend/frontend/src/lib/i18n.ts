import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';


i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    debug: true,
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false, // not needed for react as it escapes by default
    },
    resources: {
      en: {
        translation: {
          "Online Tech Store": "Online Tech Store",
          "Home": "Home",
          "Products": "Products",
          "About": "About",
          "Contact": "Contact",
          "Sign In": "Sign In",
          "toggle_language": "TR",
          "Products Page": "Products Page",
                    "Welcome to the products page!": "Welcome to the products page!",
                    "Filters": "Filters",
                    "Category": "Category",
                    "Electronics": "Electronics",
                    "Computers": "Computers",
                    "Smart Home": "Smart Home",
                              "Price": "Price",
                              "Search": "Search"
                            }
                          },
                          tr: {
                            translation: {
                              "Online Tech Store": "Online Tech Store",
                              "Home": "Anasayfa",
                              "Products": "Ürünler",
                              "About": "Hakkında",
                              "Contact": "İletişim",
                              "Sign In": "Giriş Yap",
                              "toggle_language": "EN",
                              "Products Page": "Ürünler Sayfası",
                              "Welcome to the products page!": "Ürünler sayfasına hoş geldiniz!",
                              "Filters": "Filtreler",
                              "Category": "Kategori",
                              "Electronics": "Elektronik",
                              "Computers": "Bilgisayarlar",
                              "Smart Home": "Akıllı Ev",
                              "Price": "Fiyat",
                              "Search": "Ara"}}}});


export default i18n;