import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { RolesService } from '../../../common/services/roles.service';
import { UserRole } from '../../../common/models/user-role';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [RouterModule,
    CommonModule,
    ButtonModule
  ],
  templateUrl: './roles-list.component.html',
  styleUrl: './roles-list.component.scss'
})
export class RolesListComponent implements OnInit {
  roles = new Array<UserRole>();

  constructor(
    private router: Router,
    private rolesService: RolesService,
    private cd: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.rolesService.getRoles().subscribe({
      next: (res) => {
        this.roles = res;
        this.cd.detectChanges();
      }
    });
  }

  createNewRole(): void {
    this.router.navigate([`/role`])
  }

  getRoleDetails(id: string): void {
    this.router.navigate([`/role/${id}`])
  }
}
