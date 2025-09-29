import React from 'react'
import ReactDOM from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import ControlPanel from '@/pages/ControlPanel'
import Overlay from '@/pages/Overlay'
import '@/index.css'

const router = createBrowserRouter([
  { path: '/', element: <ControlPanel /> },
  { path: '/overlay/court/:courtId', element: <Overlay /> },
])

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode><RouterProvider router={router} /></React.StrictMode>
)
