import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import type { Brand, Category } from '../lib/api';
import { api } from '../lib/api';

const FilterBar = () => {
  const { t } = useTranslation();
  const [price, setPrice] = useState(500); // Initial price value
  const [brands, setBrands] = useState<Brand[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedBrands, setSelectedBrands] = useState<number[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<number[]>([]);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [brandData, categoryData] = await Promise.all([
          api.getBrands(),
          api.getCategories(),
        ]);
        setBrands(brandData);
        setCategories(categoryData);
      } catch (error) {
        console.error("Failed to fetch filters:", error);
      }
    };

    fetchData();
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

  const handleCategoryChange = (categoryId: number) => {
    setSelectedCategories(prev =>
      prev.includes(categoryId)
        ? prev.filter(id => id !== categoryId)
        : [...prev, categoryId]
    );
  };

  const renderCategories = (categories: Category[]) => {
    return categories.map(category => (
      <React.Fragment key={category.categoryID}>
        <label>
          <input
            type="checkbox"
            name="category"
            value={category.categoryID}
            checked={selectedCategories.includes(category.categoryID)}
            onChange={() => handleCategoryChange(category.categoryID)}
          /> {category.categoryName}
        </label>
        {category.subCategories && category.subCategories.length > 0 && (
          <div style={{ marginLeft: '20px' }}>
            {renderCategories(category.subCategories)}
          </div>
        )}
      </React.Fragment>
    ));
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
        {renderCategories(categories)}
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