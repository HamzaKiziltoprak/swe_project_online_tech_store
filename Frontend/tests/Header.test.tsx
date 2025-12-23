import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import Header from '@/components/Header';

describe('Header Component', () => {

  it('Verilen başlığı ekrana basmalı', () => {

    render(<Header title="Mağazamıza Hoşgeldiniz" />);

    const titleElement = screen.getByRole('heading', { level: 1 });

    expect(titleElement).toHaveTextContent('Mağazamıza Hoşgeldiniz');
  });

  it('Doğru stil sınıflarına sahip olmalı', () => {

    render(<Header title="Test" />);

    const headerElement = screen.getByRole('banner');

    expect(headerElement).toHaveStyle({ backgroundColor: '#333' });
  });
});
