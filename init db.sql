create database deals_db;
use deals_db;

create table buyer 
(
	id int unsigned auto_increment primary key,
	name varchar(150) not null,
    inn varchar(40)
);

create table seller 
(
	id int unsigned auto_increment primary key,
	name varchar(150) not null,
    inn varchar(40) not null
);

create table deal 
(
	deal_number varchar(35) not null primary key,
    deal_date date not null,
    volume_buyer double not null,
    volume_seller double not null,
    
    
    seller_id int unsigned not null ,
    buyer_id int unsigned not null,
    foreign key (seller_id) references seller(id),
    foreign key (buyer_id) references buyer(id)
);

ALTER TABLE deal MODIFY volume_buyer decimal(15,5);
ALTER TABLE deal MODIFY volume_seller decimal(15,5);

ALTER TABLE seller MODIFY name varchar(1000);
ALTER TABLE buyer MODIFY name varchar(1000);

