import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';

const FilterBar = () => {
  const { t } = useTranslation();
  const [price, setPrice] = useState(500); // Initial price value

  const handlePriceChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setPrice(Number(event.target.value));
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