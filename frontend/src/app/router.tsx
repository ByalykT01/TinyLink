import { createBrowserRouter } from 'react-router-dom'
import { AppShell } from './layout/AppShell'
import { NotFoundPage } from './layout/NotFoundPage'
import { RedirectPage } from '../features/redirect/RedirectPage'
import { ShortenPage } from '../features/shorten-link/ShortenPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <ShortenPage /> },
      { path: ':code', element: <RedirectPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
