import { create } from 'zustand';
import { devtools } from 'zustand/middleware';

interface AppState {
  // UI State
  isSidebarOpen: boolean;
  theme: 'light' | 'dark';
  
  // User State (future)
  user: null | {
    id: string;
    name: string;
    email: string;
    role: string;
  };
  
  // Loading States
  isLoading: boolean;
  
  // Actions
  toggleSidebar: () => void;
  setTheme: (theme: 'light' | 'dark') => void;
  setUser: (user: AppState['user']) => void;
  setLoading: (loading: boolean) => void;
}

export const useAppStore = create<AppState>()(
  devtools(
    (set) => ({
      // Initial State
      isSidebarOpen: false,
      theme: 'light',
      user: null,
      isLoading: false,
      
      // Actions
      toggleSidebar: () => set((state) => ({ isSidebarOpen: !state.isSidebarOpen })),
      setTheme: (theme) => set({ theme }),
      setUser: (user) => set({ user }),
      setLoading: (isLoading) => set({ isLoading }),
    }),
    {
      name: 'app-store', // Name for devtools
    }
  )
);