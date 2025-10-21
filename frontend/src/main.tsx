import React from 'react'
import ReactDOM from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import App from '@/App'
import ControlPanel from '@/pages/ControlPanel'
import Overlay from '@/pages/Overlay'
import { ENABLE_LEGACY_APP } from '@/config'
import '@/index.css'

const routes = [
  { path: '/', element: <ControlPanel /> },
  { path: '/overlay/court/:courtId', element: <Overlay /> },
]

if (ENABLE_LEGACY_APP) {
  routes.push({ path: '/legacy', element: <App /> })
}

const router = createBrowserRouter(routes)

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>
)
