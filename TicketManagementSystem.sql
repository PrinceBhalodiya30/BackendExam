create database Ticket_Management

create table roles (
	id int identity(1,1) primary key,
    name varchar(50) not null unique check (name in ('MANAGER', 'SUPPORT', 'USER'))
);

insert into roles (name) values ('manager'), ('support'), ('user');

create table users (
    id int identity(1,1) primary key,
	name varchar(255) not null,
    email varchar(255) not null unique,
    password varchar(255) not null,
    role_id int not null foreign key references roles(id),
);

create table tickets (
	id int identity(1,1) primary key,
	title varchar(255) not null,
	description varchar(255) not null,
    status varchar(50) default 'open' check (status in ('open', 'in_progress', 'resolved', 'closed')),
    priority varchar(50) default 'medium' check (priority in ('low', 'medium', 'high')),
	created_by int not null foreign key references users(id),
	assigned_to int null foreign key references users(id),
	created_at datetime default getdate()
);

create table ticket_comments (
    id int identity(1,1) primary key,
    ticket_id int not null foreign key references tickets(id) on delete cascade,
    user_id int not null foreign key references users(id),
    comment varchar(max) not null,
    created_at datetime2 default getdate()
);


create table ticket_status_logs (
    id int identity(1,1) primary key,
    ticket_id int not null foreign key references tickets(id) on delete cascade,
    old_status varchar(50) not null check (old_status in ('open', 'in_progress', 'resolved', 'closed')),
    new_status varchar(50) not null check (new_status in ('open', 'in_progress', 'resolved', 'closed')),
    changed_by int not null foreign key references users(id),
    changed_at datetime2 default getdate()
);

select * from users
