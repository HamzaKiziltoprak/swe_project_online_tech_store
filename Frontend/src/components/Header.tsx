import React from 'react';

interface HeaderProps {
    title: string;
}

const Header: React.FC<HeaderProps> = ({ title }) => {
    return (
        <header style={{
            backgroundColor:'#333',
            color:'#fff',
            padding:'1rem',
            textAlign:'center',
            fontSize:'1.5rem'
        }}>
            <h1>{title}</h1>
        </header>
    );
};

export default Header;