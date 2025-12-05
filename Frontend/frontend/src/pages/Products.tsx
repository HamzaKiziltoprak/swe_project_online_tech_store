import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import FilterBar from '../components/FilterBar';
import '../styles/Products.css';

// Ürün veri yapısını tanımlayan interface
interface Product {
    id: number;
    name: string;
    price: number;
    imageUrl: string;
    description: string; // Liste görünümü için açıklama eklendi
}

// Örnek ürün verileri
const mockProducts: Product[] = [
    { id: 1, name: 'Laptop Pro', price: 1200, imageUrl: 'https://via.placeholder.com/150', description: 'High-performance laptop for professionals.' },
    { id: 2, name: 'Wireless Mouse', price: 50, imageUrl: 'https://via.placeholder.com/150', description: 'Ergonomic wireless mouse with long battery life.' },
    { id: 3, name: 'Mechanical Keyboard', price: 150, imageUrl: 'https://via.placeholder.com/150', description: 'RGB mechanical keyboard for gaming and typing.' },
    { id: 4, name: '4K Monitor', price: 450, imageUrl: 'https://via.placeholder.com/150', description: '27-inch 4K UHD monitor with vibrant colors.' },
    { id: 5, name: 'Gaming Headset', price: 180, imageUrl: 'https://via.placeholder.com/150', description: 'Immersive sound experience for gamers.' },
    { id: 6, name: 'USB-C Hub', price: 70, imageUrl: 'https://via.placeholder.com/150', description: 'Multi-port USB-C hub for all your devices.' },
];

const Products = () => {
    const { t } = useTranslation();
    const [products, setProducts] = useState<Product[]>([]);
    const [viewMode, setViewMode] = useState('grid'); // 'grid' veya 'list'

    useEffect(() => {
        setProducts(mockProducts);
    }, []);

    return (
        <div className="products-page-container">
            <FilterBar />
            <main className="products-list">
                <div className="view-options">
                    <h2>{t('Products')}</h2>
                    <div>
                        <button onClick={() => setViewMode('grid')} disabled={viewMode === 'grid'}>Grid</button>
                        <button onClick={() => setViewMode('list')} disabled={viewMode === 'list'}>List</button>
                    </div>
                </div>
                <div className={viewMode === 'grid' ? 'products-grid' : 'products-list-view'}>
                    {products.map((product) => (
                        <div key={product.id} className="product-card">
                            <img src={product.imageUrl} alt={product.name} className="product-image" />
                            <div className="product-details">
                                <h3 className="product-name">{product.name}</h3>
                                <p className="product-price">${product.price}</p>
                                {viewMode === 'list' && <p className="product-description">{product.description}</p>}
                            </div>
                        </div>
                    ))}
                </div>
            </main>
        </div>
    );
};

export default Products;