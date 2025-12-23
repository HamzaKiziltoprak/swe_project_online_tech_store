import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import FilterBar from '@/components/FilterBar';
import { api } from '@/lib/api';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('@/lib/api', () => ({
  api: {
    getBrands: vi.fn(),
    getCategories: vi.fn(),
  },
}));

describe('FilterBar Component', () => {

  beforeEach(() => {
    vi.clearAllMocks();

    (api.getBrands as any).mockResolvedValue([
      { brandID: 1, brandName: 'Samsung', productCount: 5 }
    ]);

    (api.getCategories as any).mockResolvedValue([
      { categoryID: 1, categoryName: 'Telefonlar', subCategories: [] }
    ]);
  });

  it('Marka ve kategorileri API üzerinden yükleyip göstermeli', async () => {

    await act(async () => {
      render(<FilterBar />);
    });
    
    expect(await screen.findByText(/Samsung/i)).toBeInTheDocument();
    expect(screen.getByText(/Telefonlar/i)).toBeInTheDocument();
  });

  it('Fiyat aralığı (range) değiştiğinde değer güncellenmeli', async () => {

    await act(async () => {
      render(<FilterBar />);
    });

    const rangeInput = screen.getByRole('slider');

    await act(async () => {
      fireEvent.change(rangeInput, { target: { value: '750' } });
    });

    expect(screen.getByText('750 TL')).toBeInTheDocument();
  });

  it('Checkbox tıklandığında işaretlenmeli', async () => {

    await act(async () => {
      render(<FilterBar />);
    });

    const brandCheckbox = await screen.findByLabelText(/Samsung/i);
   
    await act(async () => {
      fireEvent.click(brandCheckbox);
    });

    expect(brandCheckbox).toBeChecked();
  });
});
