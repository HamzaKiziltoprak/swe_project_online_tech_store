import React from 'react';
import { useTranslation } from 'react-i18next';
import FilterBar from './FilterBar';
import './Products.css';

const Products = () => {
  const { t } = useTranslation();
  return (
    <div className="products-page-container">
      <FilterBar />
      <main className="products-list">
        <h2>{t('Products Page')}</h2>
        <p>{t('Welcome to the products page!')}</p>
      </main>
    </div>
  );
};

export default Products;