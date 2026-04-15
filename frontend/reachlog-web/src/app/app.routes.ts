import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register').then(m => m.RegisterComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'kanban',
    loadComponent: () => import('./pages/kanban/kanban').then(m => m.KanbanComponent),
    canActivate: [authGuard]
  },
  {
    path: 'cv',
    loadComponent: () => import('./pages/cv-upload/cv-upload').then(m => m.CvUploadComponent),
    canActivate: [authGuard]
  },
  {
    path: 'analytics',
    loadComponent: () => import('./pages/analytics/analytics').then(m => m.AnalyticsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'outreach/new',
    loadComponent: () => import('./pages/create-outreach/create-outreach').then(m => m.CreateOutreachComponent),
    canActivate: [authGuard]
  },
  {
    path: 'outreach/:id/edit',
    loadComponent: () => import('./pages/edit-outreach/edit-outreach').then(m => m.EditOutreachComponent),
    canActivate: [authGuard]
  },
  {
    path: 'inbox',
    loadComponent: () => import('./pages/paste-inbox/paste-inbox').then(m => m.PasteInboxComponent),
    canActivate: [authGuard]
  },
  {
    path: 'prepare',
    loadComponent: () => import('./pages/prepare/prepare').then(m => m.PrepareComponent),
    canActivate: [authGuard]
  },
  {
    path: 'jobs',
    loadComponent: () => import('./pages/jobs/jobs').then(m => m.JobsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'account',
    loadComponent: () => import('./pages/account/account').then(m => m.AccountComponent),
    canActivate: [authGuard]
  },
  {
    path: '**', redirectTo: 'dashboard'
  }
];
