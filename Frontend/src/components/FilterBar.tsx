import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Brand } from '../lib/api';

const FilterBar = () => {
  const { t } = useTranslation();
  const [price, setPrice] = useState(500); // Initial price value
  const [brands, setBrands] = useState<Brand[]>([]);
  const [selectedBrands, setSelectedBrands] = useState<number[]>([]);

  useEffect(() => {
    const fetchBrands = async () => {
      try {
        const brandData = await api.getBrands();
        setBrands(brandData);
      } catch (error) {
        console.error("Failed to fetch brands:", error);
      }
    };

    fetchBrands();
  }, []);

  const handlePriceChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setPrice(Number(event.target.value));
  };

  const handleBrandChange = (brandId: number) => {
    setSelectedBrands(prev => 
      prev.includes(brandId) 
        ? prev.filter(id => id !== brandId) 
        : [...prev, brandId]
    );
  };

  return (
    <aside className="filter-bar">
      <h3>{t('Filters')}</h3>
      <div className="filter-group">
        <h4>{t('Search')}</h4>
        <input type="text" placeholder={t('Search')} className="filter-search-input" />
      </div>
      <div className="filter-group">
        <h4>{t('Category')}</h4>
        <label>
          <input type="checkbox" name="category" value="electronics" /> {t('Electronics')}
        </label>
        <label>
          <input type="checkbox" name="category" value="computers" /> {t('Computers')}
        </label>
        <label>
          <input type="checkbox" name="category" value="smart-home" /> {t('Smart Home')}
        </label>
      </div>
      <div className="filter-group">
        <h4>{t('Brands')}</h4>
        {brands.map(brand => (
          <label key={brand.brandID}>
            <input 
              type="checkbox" 
              name="brand" 
              value={brand.brandID} 
              checked={selectedBrands.includes(brand.brandID)}
              onChange={() => handleBrandChange(brand.brandID)}
            /> 
            {brand.brandName} ({brand.productCount})
          </label>
        ))}
      </div>
      <div className="filter-group">
        <h4>{t('Price')}</h4>    
        <input 
          type="range" 
          min="0" 
          max="1000" 
          value={price} 
          onChange={handlePriceChange} 
        />
        <span>{price} TL</span>
      </div>
    </aside>
  );
};

export default FilterBar;