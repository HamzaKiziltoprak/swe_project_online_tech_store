import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import NotFound from '@/pages/NotFound';

const tStable = (key: string) => key;
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: tStable }),
}));

describe('NotFound Component', () => {
  const renderNotFound = () => {
    return render(
      <MemoryRouter>
        <NotFound />
      </MemoryRouter>
    );
  };

  it('404 hata kodunu ve ilgili başlığı göstermeli', () => {
    renderNotFound();
    
    expect(screen.getByText('🚫 404')).toBeInTheDocument();
  
    expect(screen.getByRole('heading', { name: /❌ page_not_found/i })).toBeInTheDocument();
    
    expect(screen.getByText(/page_not_found_message/i)).toBeInTheDocument();
  });

  it('Ana sayfaya ve ürünlere dönüş linkleri doğru olmalı', () => {
    renderNotFound();

    const productsLink = screen.getByRole('link', { name: /back_to_products/i });
    expect(productsLink).toHaveAttribute('href', '/products');

    const homeLink = screen.getByRole('link', { name: /back_to_home/i });
    expect(homeLink).toHaveAttribute('href', '/');
  });

  it('Bileşen doğru CSS sınıflarına sahip olmalı', () => {
    const { container } = renderNotFound();
    expect(container.querySelector('.not-found-container')).toBeInTheDocument();
    expect(container.querySelector('.not-found-content')).toBeInTheDocument();
  });
});